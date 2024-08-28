// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
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

    public class ServiceInfo
    {
        public ServiceInfo(uint serviceId, Type agentType, Func<object>? createAgent)
        {
            this.ServiceId = serviceId;
            this.AgentType = agentType;
            this.CreateAgent = createAgent;
        }

        public void AddMethod(ServiceMethod serviceMethod) => this.serviceMethods.TryAdd(serviceMethod.Id, serviceMethod);

        public bool TryGetMethod(ulong id, [MaybeNullWhen(false)] out ServiceMethod serviceMethod) => this.serviceMethods.TryGetValue(id, out serviceMethod);

        public uint ServiceId { get; }

        public Type AgentType { get; }

        public Func<object>? CreateAgent { get; }

        private Dictionary<ulong, ServiceMethod> serviceMethods = new();
    }

    private readonly record struct ServiceInfoInstance(ServiceInfo ServiceInfo, object AgentInstance);

    public record class ServiceMethod
    {
        public ServiceMethod(ulong id, ServiceDelegate invoke)
        {// Id = ServiceId + MethodId
            this.Id = id;
            this.Invoke = invoke;
        }

        public ulong Id { get; }

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
    private readonly UInt32Hashtable<ServiceInfoInstance> serviceIdToBackend = new();

    #endregion

    /*public virtual bool RespondUpdateAgreement(CertificateToken<ConnectionAgreement> token)
        => false;

    public virtual bool RespondConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
        => false;*/

    /*public NetResult Authenticate(AuthenticationToken authenticationToken, SignaturePublicKey publicKey)
    {
        if (!authenticationToken.PublicKey.Equals(publicKey))
        {
            return NetResult.NotAuthenticated;
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
            return NetResult.NotAuthenticated;
        }
    }*/

    /*public bool TryGetAuthenticationToken([MaybeNullWhen(false)] out AuthenticationToken authenticationToken)
    {
        authenticationToken = this.AuthenticationToken;
        return authenticationToken is not null;
    }*/

    internal void InvokeStream(ReceiveTransmission receiveTransmission, ulong dataId, long maxStreamLength)
    {
        // Get ServiceMethod
        (var serviceMethod, var agentInstance) = this.TryGetServiceMethod(dataId);
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
                await serviceMethod.Invoke(agentInstance, transmissionContext).ConfigureAwait(false);
                try
                {
                    if (!transmissionContext.IsSent)
                    {
                        transmissionContext.CheckReceiveStream();
                        var result = transmissionContext.Result;
                        if (result == NetResult.Success)
                        {// Success
                            transmissionContext.SendAndForget(transmissionContext.RentMemory, (ulong)result);
                        }
                        else
                        {// Failure
                            transmissionContext.SendAndForget(BytePool.RentMemory.Empty, (ulong)result);
                        }
                    }
                }
                catch
                {
                }
            }
            catch
            {// Unknown exception
                transmissionContext.SendAndForget(BytePool.RentMemory.Empty, (ulong)NetResult.UnknownError);
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
            if (transmissionContext.DataId == ConnectionAgreement.AuthenticationTokenId)
            {
                this.SetAuthenticationToken(transmissionContext);
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
        (var serviceMethod, var agentInstance) = this.TryGetServiceMethod(transmissionContext.DataId);
        if (serviceMethod == null)
        {
            goto SendNoNetService;
        }

        // Invoke
        TransmissionContext.AsyncLocal.Value = transmissionContext;
        try
        {
            await serviceMethod.Invoke(agentInstance, transmissionContext).ConfigureAwait(false);
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
                        transmissionContext.SendAndForget(transmissionContext.RentMemory, (ulong)result);
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
        transmissionContext.SendAndForget(BytePool.RentMemory.Empty, (ulong)NetResult.NoNetService);
        transmissionContext.ReturnAndDisposeStream();
        return;
    }

    private void SetAuthenticationToken(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<AuthenticationToken>(transmissionContext.RentMemory.Memory.Span, out var token))
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

            transmissionContext.SendAndForget(result, ConnectionAgreement.AuthenticationTokenId);
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

    private (ServiceMethod? ServiceMethod, object AgentInstance) TryGetServiceMethod(ulong dataId)
    {//
        var serviceId = (uint)(dataId >> 32);
        var methodId = (uint)dataId;

        //var ss = this.ServiceProvider.CreateScope();
        var s = this.serviceIdToBackend.GetOrAdd(serviceId, id =>
        {
            object? agentInstance = default;
            if (this.NetTerminal.Services.TryGet(serviceId, out var info))
            {
                agentInstance = this.ServiceProvider?.GetService(info.AgentType);
                agentInstance ??= info.CreateAgent?.Invoke();
                return new(info, agentInstance);
            }

            return new(default, agentInstance);
        });

        if (s.ServiceInfo.TryGetMethod(methodId, out var serviceMethod))
        {
            return (serviceMethod, s.AgentInstance);
        }

        return default;
    }
}
