// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Block;
using Netsphere.Net;

namespace Netsphere.Server;

public class ExampleConnectionContext : ConnectionContext
{
    public ExampleConnectionContext(ServerConnection serverConnection)
        : base(serverConnection)
    {
    }

    public override async Task InvokeStream(StreamContext streamContext)
    {
        return;
    }

    public override ConnectionAgreementBlock RequestAgreement(ConnectionAgreementBlock agreement)
    {
        return this.ServerConnection.Agreement;
    }
}

public class ConnectionContext
{
    public delegate Task ServiceDelegate(object instance, TransmissionContext transmissionContext);

    public delegate INetService CreateFrontendDelegate(ClientConnection clientConnection);

    public delegate object CreateBackendDelegate(ConnectionContext connectionContext);

    public class ServiceInfo
    {
        public ServiceInfo(uint serviceId, CreateBackendDelegate createBackend)
        {
            this.ServiceId = serviceId;
            this.CreateBackend = createBackend;
        }

        public void AddMethod(ServiceMethod serviceMethod) => this.serviceMethods.TryAdd(serviceMethod.Id, serviceMethod);

        public bool TryGetMethod(ulong id, [MaybeNullWhen(false)] out ServiceMethod serviceMethod) => this.serviceMethods.TryGetValue(id, out serviceMethod);

        public uint ServiceId { get; }

        public CreateBackendDelegate CreateBackend { get; }

        private Dictionary<ulong, ServiceMethod> serviceMethods = new();
    }

    public record class ServiceMethod
    {
        public ServiceMethod(ulong id, ServiceDelegate process)
        {
            this.Id = id;
            this.Invoke = process;
        }

        public ulong Id { get; }

        public object? ServerInstance { get; init; }

        public ServiceDelegate Invoke { get; }
    }

    public ConnectionContext(ServerConnection serverConnection)
    {
        this.ServiceProvider = default; // serviceProvider;
        this.NetTerminal = serverConnection.ConnectionTerminal.NetTerminal;
        this.ServerConnection = serverConnection;
    }

    #region FieldAndProperty

    public IServiceProvider? ServiceProvider { get; }

    public NetTerminal NetTerminal { get; }

    public ServerConnection ServerConnection { get; }

    private readonly Dictionary<ulong, ServiceMethod> idToServiceMethod = new(); // lock (this.idToServiceMethod)
    private readonly Dictionary<uint, object> idToInstance = new(); // lock (this.idToServiceMethod)

    #endregion

    public virtual Task InvokeStream(StreamContext streamContext)
        => Task.CompletedTask;

    public virtual ConnectionAgreementBlock RequestAgreement(ConnectionAgreementBlock agreement)
        => this.ServerConnection.Agreement;

    /*public virtual bool InvokeCustom(TransmissionContext transmissionContext)
    {
        return false;
    }*/

    internal void InvokeSync(TransmissionContext transmissionContext)
    {// transmissionContext.Return();
        if (transmissionContext.DataKind == 0)
        {// Block (Responder)
            if (transmissionContext.DataId == ConnectionAgreementBlock.DataId)
            {
                this.AgreementRequested(transmissionContext);
            }
            else if (this.NetTerminal.NetResponder.TryGet(transmissionContext.DataId, out var responder))
            {
                responder.Respond(transmissionContext);
            }
            else
            {
                transmissionContext.Return();
                return;
            }
        }
        else if (transmissionContext.DataKind == 1)
        {// RPC
            Task.Run(() => this.InvokeRPC(transmissionContext));
            return;
        }

        /*if (!this.InvokeCustom(transmissionContext))
        {
            transmissionContext.Return();
        }*/
    }

    internal async Task InvokeRPC(TransmissionContext transmissionContext)
    {// Thread-safe
        ServiceMethod? serviceMethod;
        lock (this.idToServiceMethod)
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
                        backendInstance = serviceInfo.CreateBackend(this);
                    }
                    catch
                    {
                        goto SendNoNetService;
                    }

                    this.idToInstance.TryAdd(serviceId, backendInstance);
                }

                serviceMethod = serviceMethod with { ServerInstance = backendInstance, };
                this.idToServiceMethod.TryAdd(transmissionContext.DataId, serviceMethod);
            }
        }

        TransmissionContext.AsyncLocal.Value = transmissionContext;
        try
        {
            await serviceMethod.Invoke(serviceMethod.ServerInstance!, transmissionContext).ConfigureAwait(false);
            try
            {
                var result = transmissionContext.Result;
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

    private bool AgreementRequested(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<ConnectionAgreementBlock>(transmissionContext.Owner.Memory.Span, out var t))
        {
            transmissionContext.Return();
            return false;
        }

        transmissionContext.Return();

        var response = this.RequestAgreement(t);
        if (response != this.ServerConnection.Agreement)
        {
            this.ServerConnection.Agreement.Update(response);
        }

        transmissionContext.SendAndForget(response, ConnectionAgreementBlock.DataId);
        return true;
    }
}
