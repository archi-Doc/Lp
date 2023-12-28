// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Netsphere.Server;

namespace Netsphere;

public class NetService
{
    public delegate Task ServiceDelegate(object instance, TransmissionContext transmissionContext);

    public delegate INetService CreateFrontendDelegate(ClientTerminal clientTerminal);

    public delegate object CreateBackendDelegate(ConnectionContext connectionContext);

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
    }

    public NetService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task Process(TransmissionContext transmissionContext)
    {// Thread-safe
        ServiceMethod? serviceMethod;
        lock (this.syncObject)
        {
            if (!this.idToServiceMethod.TryGetValue(transmissionContext.DataId, out serviceMethod))
            {
                // Get ServiceInfo.
                var serviceId = (uint)(transmissionContext.DataId >> 32);
                if (!StaticNetService.TryGetServiceInfo(serviceId, out var serviceInfo))
                {
                    goto SendNoNetService;
                }

                // Get ServiceMethod.
                if (!serviceInfo.TryGetMethod(transmissionContext.DataId, out serviceMethod))
                {
                    goto SendNoNetService;
                }

                // Get Backend instance.
                if (!this.idToInstance.TryGetValue(serviceId, out var backendInstance))
                {
                    try
                    {
                        backendInstance = serviceInfo.CreateBackend(this.ConnectionContext);
                    }
                    catch
                    {
                        goto SendNoNetService;
                    }

                    this.idToInstance.TryAdd(serviceId, backendInstance);
                }

                serviceMethod = serviceMethod.CloneWithInstance(backendInstance);
                this.idToServiceMethod.TryAdd(transmissionContext.DataId, serviceMethod);
            }
        }

        // context.Initialize(this.ConnectionContext, rent.Received.IncrementAndShare(), rent.DataId);
        // CallContext.CurrentCallContext.Value = context;
        try
        {
            await serviceMethod.Invoke(serviceMethod.ServerInstance!, transmissionContext).ConfigureAwait(false);
            try
            {
                var result = NetResult.Success; // context.Result
                if (result == NetResult.Success)
                {// Success
                    transmissionContext.SendAndForget(transmissionContext.Owner, (ulong)result);
                }
                else
                {// Failure

                    transmissionContext.SendAndForget(ByteArrayPool.MemoryOwner.Empty, (ulong)result);
                }
            }
            catch
            {
            }
        }
        catch (NetException netException)
        {// NetException
            transmissionContext.SendAndForget(ByteArrayPool.MemoryOwner.Empty, (ulong)netException.Result);
        }
        catch
        {// Unknown exception
            transmissionContext.SendAndForget(ByteArrayPool.MemoryOwner.Empty, (ulong)NetResult.UnknownException);
        }
        finally
        {
            transmissionContext.Return();
        }

        return;

SendNoNetService:
        transmissionContext.SendAndForget(ByteArrayPool.MemoryOwner.Empty, (ulong)NetResult.NoNetService);
        transmissionContext.Return();
        return;
    }

    public ConnectionContext ConnectionContext { get; internal set; } = default!;

    public Func<CallContext> NewCallContext { get; internal set; } = default!;

    private object syncObject = new();
    private IServiceProvider serviceProvider;
    private Dictionary<ulong, ServiceMethod> idToServiceMethod = new();
    private Dictionary<uint, object> idToInstance = new();
}
