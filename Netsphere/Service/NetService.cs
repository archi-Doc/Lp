// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Netsphere;

public class NetService
{
    public delegate Task ServiceDelegate(object instance, CallContext context);

    public delegate INetService CreateFrontendDelegate(ClientTerminal clientTerminal);

    public delegate object CreateBackendDelegate(IServiceProvider? serviceProvider, ServerContext serviceContext);

    public class ServiceInfo
    {
        public ServiceInfo(uint serviceId, CreateBackendDelegate createBackend)
        {
            this.ServiceId = serviceId;
            this.CreateBackend = createBackend;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMethod(ServiceMethod serviceMethod) => this.serviceMethods.TryAdd(serviceMethod.Id, serviceMethod);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetMethod(ulong id, [MaybeNullWhen(false)] out ServiceMethod serviceMethod) => this.serviceMethods.TryGetValue(id, out serviceMethod);

        public uint ServiceId { get; }

        public CreateBackendDelegate CreateBackend { get; }

        private Dictionary<ulong, ServiceMethod> serviceMethods = new();
    }

    public class ServiceMethod
    {
        public ServiceMethod(ulong id, ServiceDelegate process)
        {
            this.Id = id;
            this.Invoke = process;
        }

        public ulong Id { get; }

        public object? ServerInstance { get; private set; }

        public ServiceDelegate Invoke { get; }

        public ServiceMethod CloneWithInstance(object serverInstance)
        {
            var serviceMethod = new ServiceMethod(this.Id, this.Invoke);
            serviceMethod.ServerInstance = serverInstance;
            return serviceMethod;
        }

        public NetTask<ByteArrayPool.MemoryOwner> FilterAndProcess(object instance, ByteArrayPool.MemoryOwner received)
        {
            return default;
        }
    }

    public NetService(IServiceProvider? serviceProvider = null)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task Process(ServerTerminal serverTerminal, NetReceivedData rent)
    {// Thread-safe
        ServiceMethod? serviceMethod;
        lock (this.syncObject)
        {
            if (!this.idToServiceMethod.TryGetValue(rent.DataId, out serviceMethod))
            {
                // Get ServiceInfo.
                var serviceId = (uint)(rent.DataId >> 32);
                if (!StaticNetService.TryGetServiceInfo(serviceId, out var serviceInfo))
                {
                    goto SendNoNetService;
                }

                // Get ServiceMethod.
                if (!serviceInfo.TryGetMethod(rent.DataId, out serviceMethod))
                {
                    goto SendNoNetService;
                }

                // Get Backend instance.
                if (!this.idToInstance.TryGetValue(serviceId, out var backendInstance))
                {
                    try
                    {
                        backendInstance = serviceInfo.CreateBackend(this.serviceProvider, this.ServerContext);
                    }
                    catch
                    {
                        goto SendNoNetService;
                    }

                    this.idToInstance.TryAdd(serviceId, backendInstance);
                }

                serviceMethod = serviceMethod.CloneWithInstance(backendInstance);
                this.idToServiceMethod.TryAdd(rent.DataId, serviceMethod);
            }
        }

        var context = this.NewCallContext();
        context.Initialize(this.ServerContext, rent.Received.IncrementAndShare());
        CallContext.CurrentCallContext.Value = context;
        try
        {
            await serviceMethod.Invoke(serviceMethod.ServerInstance!, context);
            try
            {
                await serverTerminal.SendServiceAsync((ulong)context.Result, context.RentData).ConfigureAwait(false);
            }
            catch
            {
            }
        }
        catch (NetException ne)
        {
            await serverTerminal.SendServiceAsync((ulong)ne.Result, ByteArrayPool.MemoryOwner.Empty).ConfigureAwait(false);
        }
        catch
        {
            await serverTerminal.SendServiceAsync((ulong)NetResult.UnknownException, ByteArrayPool.MemoryOwner.Empty).ConfigureAwait(false);
        }
        finally
        {
            context.RentData.Return();
        }

        rent.Return();
        return;

SendNoNetService:
        await serverTerminal.SendServiceAsync((ulong)NetResult.NoNetService, ByteArrayPool.MemoryOwner.Empty).ConfigureAwait(false);
        rent.Return();
        return;
    }

    public ServerContext ServerContext { get; internal set; } = default!;

    public Func<CallContext> NewCallContext { get; internal set; } = default!;

    private object syncObject = new();
    private IServiceProvider? serviceProvider;
    private Dictionary<ulong, ServiceMethod> idToServiceMethod = new();
    private Dictionary<uint, object> idToInstance = new();
}
