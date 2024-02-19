// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using Netsphere.Net;

namespace Netsphere.Server;

public class ExampleConnectionContext : ServerConnectionContext
{
    public ExampleConnectionContext(ServerConnection serverConnection)
        : base(serverConnection)
    {
    }

    public override NetResult RespondUpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {// Accept all agreement.
        return NetResult.Success;
    }

    public override NetResult RespondConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
    {// Enable bidirectional connection.
        return NetResult.Success;
    }
}

public class ServerConnectionContext
{
    #region Service

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

    #endregion

    #region FieldAndProperty

    public IServiceProvider? ServiceProvider { get; }

    public NetTerminal NetTerminal { get; }

    public ServerConnection ServerConnection { get; }

    private readonly Dictionary<ulong, ServiceMethod> idToServiceMethod = new(); // lock (this.idToServiceMethod)
    private readonly Dictionary<uint, object> idToInstance = new(); // lock (this.idToServiceMethod)

    #endregion

    public virtual NetResult RespondUpdateAgreement(CertificateToken<ConnectionAgreement> token)
        => NetResult.NotAuthorized;

    public virtual NetResult RespondConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
        => NetResult.NotAuthorized;

    /*public virtual bool InvokeBidirectional(ulong dataId)
    {
        if (dataId == 1)
        {
            this.ServerConnection.PrepareBidirectional();
            _ = Task.Run(() => { });
            return true;
        }

        return false;
    }*/

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
                        else */
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
        });
    }

    internal void InvokeSync(TransmissionContext transmissionContext)
    {// transmissionContext.Return();
        if (transmissionContext.DataKind == 0)
        {// Block (Responder)
            if (transmissionContext.DataId == ConnectionAgreement.UpdateId)
            {
                this.UpdateAgreement(transmissionContext);
            }
            else if (transmissionContext.DataId == ConnectionAgreement.BidirectionalId)
            {
                this.ConnectBidirectionally(transmissionContext);
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

        /*else if (transmissionContext.DataKind == 2)
        {// Bidirectional
            NetResult result;
            if (this.InvokeBidirectional(transmissionContext.DataId))
            {// Accepted
                result = NetResult.Success;
            }
            else
            {// Rejected
                result = NetResult.NotAuthorized;
            }

            transmissionContext.SendAndForget(ByteArrayPool.MemoryOwner.Empty, (ulong)result);
            transmissionContext.Return();
            return;
        }*/

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

    private bool UpdateAgreement(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<CertificateToken<ConnectionAgreement>>(transmissionContext.Owner.Memory.Span, out var token))
        {
            transmissionContext.Return();
            return false;
        }

        transmissionContext.Return();
        if (!this.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return false;
        }

        var result = this.RespondUpdateAgreement(token.Target);
        if (result == NetResult.Success)
        {
            this.ServerConnection.Agreement.AcceptAll(token.Target);
        }

        transmissionContext.SendAndForget(result, ConnectionAgreement.UpdateId);
        return true;
    }

    private bool ConnectBidirectionally(TransmissionContext transmissionContext)
    {
        TinyhandSerializer.TryDeserialize<CertificateToken<ConnectionAgreement>>(transmissionContext.Owner.Memory.Span, out var token);
        transmissionContext.Return();

        var result = this.RespondConnectBidirectionally(token);
        if (result == NetResult.Success)
        {
        }

        transmissionContext.SendAndForget(result, ConnectionAgreement.BidirectionalId);
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
