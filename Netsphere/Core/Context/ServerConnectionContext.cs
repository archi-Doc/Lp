// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Core;
using Netsphere.Crypto;
using Netsphere.Packet;

#pragma warning disable SA1202

namespace Netsphere;

internal class ExampleConnectionContext : ServerConnectionContext
{
    public ExampleConnectionContext(ServerConnection serverConnection)
        : base(serverConnection)
    {
    }

    /*public override bool RespondUpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {// Accept all agreement.
        if (!this.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return false;
        }

        return true;
    }

    public override bool RespondConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
    {// Enable bidirectional connection.
        if (token is null ||
            !this.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return false;
        }

        return true;
    }*/
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

    public AuthenticationToken? AuthenticationToken { get; private set; }

    private readonly Dictionary<ulong, ServiceMethod> idToServiceMethod = new(); // lock (this.idToServiceMethod)
    private readonly Dictionary<uint, object> idToInstance = new(); // lock (this.idToServiceMethod)

    #endregion

    /*public virtual bool RespondUpdateAgreement(CertificateToken<ConnectionAgreement> token)
        => false;

    public virtual bool RespondConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
        => false;*/

    public NetResult Authenticate(AuthenticationToken authenticationToken, SignaturePublicKey publicKey)
    {
        if (!authenticationToken.PublicKey.Equals(publicKey))
        {
            return NetResult.NotAuthorized;
        }

        return this.Authenticate(authenticationToken);
    }

    public NetResult Authenticate(AuthenticationToken authenticationToken)
    {
        if (this.ServerConnection.ValidateAndVerifyWithSalt(authenticationToken))
        {
            this.AuthenticationToken = authenticationToken;
            return NetResult.Success;
        }
        else
        {
            return NetResult.NotAuthorized;
        }
    }

    public bool TryGetAuthenticationToken([MaybeNullWhen(false)] out AuthenticationToken authenticationToken)
    {
        authenticationToken = this.AuthenticationToken;
        return authenticationToken is not null;
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
            transmissionContext.ReturnAndDisposeStream();
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
                        transmissionContext.CheckReceiveStream();
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
            catch
            {// Unknown exception
                transmissionContext.SendAndForget(ByteArrayPool.MemoryOwner.Empty, (ulong)NetResult.UnknownError);
            }
            finally
            {
                transmissionContext.ReturnAndDisposeStream();
            }
        });
    }

    internal void InvokeSync(TransmissionContext transmissionContext)
    {// transmissionContext.Return();
        if (transmissionContext.DataKind == 0)
        {// Block (Responder)
            if (transmissionContext.DataId == ConnectionAgreement.AuthenticateId)
            {
                this.Authenticate(transmissionContext);
            }
            else if (this.NetTerminal.Responders.TryGet(transmissionContext.DataId, out var responder))
            {
                responder.Respond(transmissionContext);
            }
            else
            {
                transmissionContext.ReturnAndDisposeStream();
                return;
            }

            /*else if (transmissionContext.DataId == CreateRelayBlock.DataId)
            {
                this.NetTerminal.RelayControl.ProcessCreateRelay(transmissionContext);
            }
            else if (transmissionContext.DataId == ConnectionAgreement.UpdateId)
            {
                this.UpdateAgreement(transmissionContext);
            }
            else if (transmissionContext.DataId == ConnectionAgreement.BidirectionalId)
            {
                this.ConnectBidirectionally(transmissionContext);
            }*/
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
                    transmissionContext.CheckReceiveStream();
                    var result = transmissionContext.Result;
                    if (result == NetResult.Success)
                    {// Success
                        transmissionContext.SendAndForget(transmissionContext.Owner, (ulong)result);
                    }
                    else
                    {// Failure
                        transmissionContext.SendResultAndForget(result);
                    }
                }
            }
            catch
            {
            }
        }
        catch
        {// Unknown exception
            transmissionContext.SendResultAndForget(NetResult.UnknownError);
        }
        finally
        {
            transmissionContext.ReturnAndDisposeStream();
        }

        return;

SendNoNetService:
        transmissionContext.SendAndForget(ByteArrayPool.MemoryOwner.Empty, (ulong)NetResult.NoNetService);
        transmissionContext.ReturnAndDisposeStream();
        return;
    }

    private void Authenticate(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<AuthenticationToken>(transmissionContext.Owner.Memory.Span, out var token))
        {
            transmissionContext.Return();
            return;
        }

        transmissionContext.Return();

        _ = Task.Run(() =>
        {
            var result = NetResult.Success;
            if (this.AuthenticationToken is null)
            {
                if (this.ServerConnection.ValidateAndVerifyWithSalt(token))
                {
                    this.AuthenticationToken = token;
                }
                else
                {
                    result = NetResult.InvalidData;
                }
            }

            transmissionContext.SendAndForget(result, ConnectionAgreement.AuthenticateId);
        });
    }

    /*private void UpdateAgreement(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<CertificateToken<ConnectionAgreement>>(transmissionContext.Owner.Memory.Span, out var token))
        {
            transmissionContext.Return();
            return;
        }

        transmissionContext.Return();

        _ = Task.Run(() =>
        {
            var result = this.RespondUpdateAgreement(token);
            if (result)
            {
                this.ServerConnection.Agreement.AcceptAll(token.Target);
                this.ServerConnection.ApplyAgreement();
            }

            transmissionContext.SendAndForget(result, ConnectionAgreement.UpdateId);
        });
    }

    private void ConnectBidirectionally(TransmissionContext transmissionContext)
    {
        TinyhandSerializer.TryDeserialize<CertificateToken<ConnectionAgreement>>(transmissionContext.Owner.Memory.Span, out var token);
        transmissionContext.Return();

        _ = Task.Run(() =>
        {
            var result = this.RespondConnectBidirectionally(token);
            if (result)
            {
                this.ServerConnection.Agreement.EnableBidirectionalConnection = true;
            }

            transmissionContext.SendAndForget(result, ConnectionAgreement.BidirectionalId);
        });
    }*/

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
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
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
