// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Block;
using Netsphere.Net;

namespace Netsphere.Server;

public class ExampleConnectionContext : ServerConnectionContext
{
    public ExampleConnectionContext(ServerConnection serverConnection)
        : base(serverConnection)
    {
    }

    public override ConnectionAgreementBlock RequestAgreement(ConnectionAgreementBlock agreement)
    {
        return this.ServerConnection.Agreement;
    }
}

public class ServerConnectionContext
{
    public delegate Task ServiceDelegate(object instance, TransmissionContext transmissionContext);

    public delegate INetService CreateFrontendDelegate(ClientConnection clientConnection);

    public delegate object CreateBackendDelegate(ServerConnectionContext connectionContext);

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

    public ServerConnectionContext(ServerConnection serverConnection)
    {
        this.ServiceProvider = serverConnection.ConnectionTerminal.ServiceProvider;
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

    public virtual ConnectionAgreementBlock RequestAgreement(ConnectionAgreementBlock agreement)
        => this.ServerConnection.Agreement;

    public virtual bool InvokeBidirectional(ulong dataId)
    {
        if (dataId == 1)
        {
            this.ServerConnection.PrepareBidirectional();
            _ = Task.Run(() => { });
            return true;
        }

        return false;
    }

    internal void InvokeStream(ReceiveTransmission receiveTransmission, ulong dataId, long maxStreamLength)
    {
        // Get ServiceMethod
        var serviceMethod = this.TryGetServiceMethod(dataId);
        if (serviceMethod is null)
        {
            return;
        }

        var transmissionContext = new TransmissionContext(this.ServerConnection, receiveTransmission.TransmissionId, 1, dataId, default);
        if (!transmissionContext.CreateReceiveStream(receiveTransmission, maxStreamLength))
        {
            transmissionContext.Return();
            receiveTransmission.Dispose();
            return;
        }

        // Invoke
        Task.Run(async () =>
        {
            TransmissionContext.AsyncLocal.Value = transmissionContext;
            try
            {
                await serviceMethod.Invoke(serviceMethod.ServerInstance!, transmissionContext).ConfigureAwait(false);
                try
                {
                    if (!transmissionContext.IsSent)
                    {
                        var result = transmissionContext.Result;
                        /*if (transmissionContext.Connection.IsClosedOrDisposed)
                        {
                            transmissionContext.ForceSendAndForget(ByteArrayPool.MemoryOwner.Empty, (ulong)NetResult.Closed);
                        }
                        else */if (result == NetResult.Success)
                        {// Success
                            transmissionContext.SendAndForget(transmissionContext.Owner, (ulong)result);
                        }
                        else
                        {// Failure
                            transmissionContext.SendAndForget(ByteArrayPool.MemoryOwner.Empty, (ulong)result);
                        }
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
        });
    }

    internal void InvokeSync(TransmissionContext transmissionContext)
    {// transmissionContext.Return();
        if (transmissionContext.DataKind == 0)
        {// Block (Responder)
            if (transmissionContext.DataId == ConnectionAgreementBlock.DataId)
            {
                this.AgreementRequested(transmissionContext);
            }
            else if (this.NetTerminal.Responders.TryGet(transmissionContext.DataId, out var responder))
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
    {
        // Get ServiceMethod
        var serviceMethod = this.TryGetServiceMethod(transmissionContext.DataId);
        if (serviceMethod == null)
        {
            goto SendNoNetService;
        }

        // Invoke
        TransmissionContext.AsyncLocal.Value = transmissionContext;
        try
        {
            await serviceMethod.Invoke(serviceMethod.ServerInstance!, transmissionContext).ConfigureAwait(false);
            try
            {
                if (transmissionContext.ServerConnection.IsClosedOrDisposed)
                {
                }
                else if (!transmissionContext.IsSent)
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

    private ServiceMethod? TryGetServiceMethod(ulong dataId)
    {
        ServiceMethod? serviceMethod;
        lock (this.idToServiceMethod)
        {
            if (!this.idToServiceMethod.TryGetValue(dataId, out serviceMethod))
            {
                // Get ServiceInfo.
                var serviceId = (uint)(dataId >> 32);
                if (!this.NetTerminal.Services.TryGet(serviceId, out var serviceInfo))
                {
                    return null;
                }

                // Get ServiceMethod.
                if (!serviceInfo.TryGetMethod(dataId, out serviceMethod))
                {
                    return null;
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
                        return null;
                    }

                    this.idToInstance.TryAdd(serviceId, backendInstance);
                }

                serviceMethod = serviceMethod with { ServerInstance = backendInstance, };
                this.idToServiceMethod.TryAdd(dataId, serviceMethod);
            }
        }

        return serviceMethod;
    }
}
