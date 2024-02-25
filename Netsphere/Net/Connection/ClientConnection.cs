﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Netsphere.Crypto;
using Netsphere.Internal;
using Netsphere.Net;
using Netsphere.Packet;

namespace Netsphere;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class ClientConnection : Connection, IClientConnectionInternal, IEquatable<ClientConnection>, IComparable<ClientConnection>
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId")]
    [Link(Type = ChainType.Unordered, Name = "DestinationEndPoint", TargetMember = "DestinationEndPoint")]
    internal ClientConnection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetNode node, NetEndPoint endPoint)
        : base(packetTerminal, connectionTerminal, connectionId, node, endPoint)
    {
        this.context = this.NetBase.NewClientConnectionContext(this);
    }

    internal ClientConnection(ServerConnection serverConnection)
        : base(serverConnection)
    {
        this.context = this.NetBase.NewClientConnectionContext(this);
        this.BidirectionalConnection = serverConnection;
    }

    #region FieldAndProperty

    public override bool IsClient => true;

    public override bool IsServer => false;

    public ServerConnection? BidirectionalConnection { get; internal set; } // lock (this.ConnectionTerminal.serverConnections.SyncObject)

    private int openCount;

    private ClientConnectionContext context;

    #endregion

    public ClientConnectionContext GetContext()
        => this.context;

    public TContext GetContext<TContext>()
        where TContext : ClientConnectionContext
        => (TContext)this.context;

    public TService GetService<TService>()
        where TService : INetService
    {
        return StaticNetService.CreateClient<TService>(this);
    }

    public async Task<NetResult> Send<TSend>(TSend data, ulong dataId = 0)
    {
        if (this.IsClosedOrDisposed)
        {
            return NetResult.Closed;
        }
        else if (this.CancellationToken.IsCancellationRequested)
        {
            return NetResult.Canceled;
        }

        if (!NetHelper.TrySerialize(data, out var owner))
        {
            return NetResult.SerializationError;
        }

        var timeout = this.NetBase.DefaultSendTimeout;
        using (var transmissionAndTimeout = await this.TryCreateSendTransmission(timeout).ConfigureAwait(false))
        {
            if (transmissionAndTimeout.Transmission is null)
            {
                owner.Return();
                return NetResult.NoTransmission;
            }

            var tcs = new TaskCompletionSource<NetResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = transmissionAndTimeout.Transmission.SendBlock(0, dataId, owner, tcs);
            owner.Return();
            if (result != NetResult.Success)
            {
                return result;
            }

            try
            {
                result = await tcs.Task.WaitAsync(transmissionAndTimeout.Timeout, this.CancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return NetResult.Timeout;
            }
            catch
            {
                return NetResult.Canceled;
            }

            return result;
        }
    }

    public async Task<NetResultValue<TReceive>> SendAndReceive<TSend, TReceive>(TSend data, ulong dataId = 0)
    {
        if (this.IsClosedOrDisposed)
        {
            return new(NetResult.Closed);
        }
        else if (this.CancellationToken.IsCancellationRequested)
        {
            return new(NetResult.Canceled);
        }

        dataId = dataId != 0 ? dataId : NetHelper.GetDataId<TSend, TReceive>();
        if (!NetHelper.TrySerialize(data, out var owner))
        {
            return new(NetResult.SerializationError);
        }

        NetResponse response;
        var timeout = this.NetBase.DefaultSendTimeout;
        using (var transmissionAndTimeout = await this.TryCreateSendTransmission(timeout).ConfigureAwait(false))
        {
            if (transmissionAndTimeout.Transmission is null)
            {
                owner.Return();
                return new(NetResult.NoTransmission);
            }

            var result = transmissionAndTimeout.Transmission.SendBlock(0, dataId, owner, default);
            owner.Return();
            if (result != NetResult.Success)
            {
                return new(result);
            }

            var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var receiveTransmission = this.TryCreateReceiveTransmission(transmissionAndTimeout.Transmission.TransmissionId, tcs))
            {
                if (receiveTransmission is null)
                {
                    return new(NetResult.NoTransmission);
                }

                try
                {
                    response = await tcs.Task.WaitAsync(transmissionAndTimeout.Timeout, this.CancellationToken).ConfigureAwait(false);
                    if (response.IsFailure)
                    {
                        return new(response.Result);
                    }
                }
                catch (TimeoutException)
                {
                    return new(NetResult.Timeout);
                }
                catch
                {
                    return new(NetResult.Canceled);
                }
            }
        }

        if (!NetHelper.TryDeserialize<TReceive>(response.Received, out var receive))
        {
            response.Return();
            return new(NetResult.DeserializationError);
        }

        response.Return();
        return new(NetResult.Success, receive);
    }

    public async Task<(NetResult Result, ulong DataId, ByteArrayPool.MemoryOwner Value)> RpcSendAndReceive(ByteArrayPool.MemoryOwner data, ulong dataId)
    {
        if (this.IsClosedOrDisposed)
        {
            return new(NetResult.Closed, 0, default);
        }
        else if (this.CancellationToken.IsCancellationRequested)
        {
            return new(NetResult.Canceled, 0, default);
        }

        NetResponse response;
        var timeout = this.NetBase.DefaultSendTimeout;
        using (var transmissionAndTimeout = await this.TryCreateSendTransmission(timeout).ConfigureAwait(false))
        {
            if (transmissionAndTimeout.Transmission is null)
            {
                return new(NetResult.NoTransmission, 0, default);
            }

            var result = transmissionAndTimeout.Transmission.SendBlock(1, dataId, data, default);
            if (result != NetResult.Success)
            {
                return new(result, 0, default);
            }

            var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var receiveTransmission = this.TryCreateReceiveTransmission(transmissionAndTimeout.Transmission.TransmissionId, tcs))
            {
                if (receiveTransmission is null)
                {
                    return new(NetResult.NoTransmission, 0, default);
                }

                try
                {
                    response = await tcs.Task.WaitAsync(transmissionAndTimeout.Timeout, this.CancellationToken).ConfigureAwait(false);
                    if (response.IsFailure)
                    {
                        return new(response.Result, 0, default);
                    }
                }
                catch (TimeoutException)
                {
                    return new(NetResult.Timeout, 0, default);
                }
                catch
                {
                    return new(NetResult.Canceled, 0, default);
                }
            }
        }

        return new(NetResult.Success, response.DataId, response.Received);
    }

    public async Task<(NetResult Result, ReceiveStream? Stream)> RpcSendAndReceiveStream(ByteArrayPool.MemoryOwner data, ulong dataId)
    {
        if (this.IsClosedOrDisposed)
        {
            return (NetResult.Closed, default);
        }
        else if (this.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Canceled, default);
        }

        NetResponse response;
        ReceiveTransmission? receiveTransmission;
        var timeout = this.NetBase.DefaultSendTimeout;
        using (var transmissionAndTimeout = await this.TryCreateSendTransmission(timeout).ConfigureAwait(false))
        {
            if (transmissionAndTimeout.Transmission is null)
            {
                return (NetResult.NoTransmission, default);
            }

            var result = transmissionAndTimeout.Transmission.SendBlock(1, dataId, data, default);
            if (result != NetResult.Success)
            {
                return (result, default);
            }

            var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            receiveTransmission = this.TryCreateReceiveTransmission(transmissionAndTimeout.Transmission.TransmissionId, tcs);
            if (receiveTransmission is null)
            {
                return (NetResult.NoTransmission, default);
            }

            try
            {
                response = await tcs.Task.WaitAsync(transmissionAndTimeout.Timeout, this.CancellationToken).ConfigureAwait(false);
                if (response.IsFailure || !response.Received.IsEmpty)
                {// Failure or not stream.
                    receiveTransmission.Dispose();
                    return new(response.Result, default);
                }
            }
            catch (TimeoutException)
            {
                receiveTransmission.Dispose();
                return (NetResult.Timeout, default);
            }
            catch
            {
                receiveTransmission.Dispose();
                return (NetResult.Canceled, default);
            }
        }

        if (response.Additional == 0)
        {// No stream
            return ((NetResult)response.DataId, default);
        }

        var stream = new ReceiveStream(receiveTransmission, response.DataId, response.Additional);
        return new(NetResult.Success, stream);
    }

    public async Task<(NetResult Result, SendStream? Stream)> SendStream(long maxLength, ulong dataId = 0)
    {
        if (this.IsClosedOrDisposed)
        {
            return (NetResult.Closed, default);
        }
        else if (this.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Canceled, default);
        }

        if (!this.Agreement.CheckStreamLength(maxLength))
        {
            return (NetResult.StreamLengthLimit, default);
        }

        var timeout = this.NetBase.DefaultSendTimeout;
        var transmissionAndTimeout = await this.TryCreateSendTransmission(timeout).ConfigureAwait(false);
        if (transmissionAndTimeout.Transmission is null)
        {
            return (NetResult.NoTransmission, default);
        }

        var tcs = new TaskCompletionSource<NetResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = transmissionAndTimeout.Transmission.SendStream(maxLength, tcs);
        if (result != NetResult.Success)
        {
            transmissionAndTimeout.Transmission.Dispose();
            return (result, default);
        }

        return (NetResult.Success, new SendStream(transmissionAndTimeout.Transmission, maxLength, dataId));
    }

    public async Task<(NetResult Result, SendStreamAndReceive<TReceive>? Stream)> SendStreamAndReceive<TReceive>(long maxLength, ulong dataId = 0)
    {
        if (this.IsClosedOrDisposed)
        {
            return (NetResult.Closed, default);
        }
        else if (this.CancellationToken.IsCancellationRequested)
        {
            return new(NetResult.Canceled, default);
        }

        if (!this.Agreement.CheckStreamLength(maxLength))
        {
            return new(NetResult.StreamLengthLimit, default);
        }

        var timeout = this.NetBase.DefaultSendTimeout;
        var transmissionAndTimeout = await this.TryCreateSendTransmission(timeout).ConfigureAwait(false);
        if (transmissionAndTimeout.Transmission is null)
        {
            return new(NetResult.NoTransmission, default);
        }

        var result = transmissionAndTimeout.Transmission.SendStream(maxLength, default);
        if (result != NetResult.Success)
        {
            transmissionAndTimeout.Transmission.Dispose();
            return new(result, default);
        }

        return new(NetResult.Success, new SendStreamAndReceive<TReceive>(transmissionAndTimeout.Transmission, maxLength, dataId));
    }

    public async Task<(NetResult Result, ReceiveStream? Stream)> SendAndReceiveStream<TSend>(TSend packet, ulong dataId = 0)
    {
        if (this.IsClosedOrDisposed)
        {
            return (NetResult.Closed, default);
        }
        else if (this.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Canceled, default);
        }

        if (!NetHelper.TrySerialize(packet, out var owner))
        {
            return (NetResult.SerializationError, default);
        }

        NetResponse response;
        ReceiveTransmission? receiveTransmission;
        var timeout = this.NetBase.DefaultSendTimeout;
        using (var transmissionAndTimeout = await this.TryCreateSendTransmission(timeout).ConfigureAwait(false))
        {
            if (transmissionAndTimeout.Transmission is null)
            {
                owner.Return();
                return (NetResult.NoTransmission, default);
            }

            var result = transmissionAndTimeout.Transmission.SendBlock(0, dataId, owner, default);
            owner.Return();
            if (result != NetResult.Success)
            {
                return (result, default);
            }

            var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            receiveTransmission = this.TryCreateReceiveTransmission(transmissionAndTimeout.Transmission.TransmissionId, tcs);
            if (receiveTransmission is null)
            {
                return (NetResult.NoTransmission, default);
            }

            try
            {
                response = await tcs.Task.WaitAsync(transmissionAndTimeout.Timeout, this.CancellationToken).ConfigureAwait(false);
                if (response.IsFailure || !response.Received.IsEmpty)
                {// Failure or not stream.
                    receiveTransmission.Dispose();
                    return new(response.Result, default);
                }
            }
            catch (TimeoutException)
            {
                receiveTransmission.Dispose();
                return (NetResult.Timeout, default);
            }
            catch
            {
                receiveTransmission.Dispose();
                return (NetResult.Canceled, default);
            }
        }

        if (response.Additional == 0)
        {// No stream
            return ((NetResult)response.DataId, default);
        }

        var stream = new ReceiveStream(receiveTransmission, response.DataId, response.Additional);
        return new(NetResult.Success, stream);
    }

    /*public async Task<NetResult> UpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {
        var r = await this.SendAndReceive<CertificateToken<ConnectionAgreement>, bool>(token, ConnectionAgreement.UpdateId).ConfigureAwait(false);
        if (r.Result == NetResult.Success && r.Value)
        {
            this.Agreement.AcceptAll(token.Target);
            this.ApplyAgreement();
        }

        return r.Result;
    }

    public async Task<NetResult> ConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
    {
        this.PrepareBidirectionalConnection(); // Create the ServerConnection in advance, as packets may not arrive in order.

        var r = await this.SendAndReceive<CertificateToken<ConnectionAgreement>?, bool>(token, ConnectionAgreement.BidirectionalId).ConfigureAwait(false);
        if (r.Result == NetResult.Success)
        {
            if (r.Value)
            {
                this.Agreement.EnableBidirectionalConnection = true;
                return NetResult.Success;
            }
            else
            {
                return NetResult.NotAuthorized;
            }
        }

        return r.Result;
    }*/

    async Task<ServiceResponse<NetResult>> IClientConnectionInternal.UpdateAgreement(ulong dataId, CertificateToken<ConnectionAgreement> a1)
    {
        if (!NetHelper.TrySerialize(a1, out var owner))
        {
            return new(NetResult.SerializationError, NetResult.SerializationError);
        }

        var response = await this.RpcSendAndReceive(owner, dataId).ConfigureAwait(false);
        owner.Return();

        try
        {
            if (response.Result != NetResult.Success)
            {
                return new(response.Result, response.Result);
            }

            if (!NetHelper.TryDeserializeNetResult(response.Value.Memory.Span, out var result))
            {
                return new(NetResult.DeserializationError, NetResult.DeserializationError);
            }

            if (result == NetResult.Success)
            {
                this.Agreement.AcceptAll(a1.Target);
                this.ApplyAgreement();
            }

            return new(result, result);
        }
        finally
        {
            response.Value.Return();
        }
    }

    async Task<ServiceResponse<NetResult>> IClientConnectionInternal.ConnectBidirectionally(ulong dataId, CertificateToken<ConnectionAgreement>? a1)
    {
        if (!NetHelper.TrySerialize(a1, out var owner))
        {
            return new(NetResult.SerializationError, NetResult.SerializationError);
        }

        this.PrepareBidirectionalConnection(); // Create the ServerConnection in advance, as packets may not arrive in order.
        var response = await this.RpcSendAndReceive(owner, dataId).ConfigureAwait(false);
        owner.Return();

        try
        {
            if (response.Result != NetResult.Success)
            {
                return new(response.Result, response.Result);
            }

            if (!NetHelper.TryDeserializeNetResult(response.Value.Memory.Span, out var result))
            {
                return new(NetResult.DeserializationError, NetResult.DeserializationError);
            }

            if (result == NetResult.Success)
            {
                this.Agreement.EnableBidirectionalConnection = true;
                if (a1 is not null)
                {
                    this.Agreement.AcceptAll(a1.Target);
                    this.ApplyAgreement();
                }
            }

            return new(result, result);
        }
        finally
        {
            response.Value.Return();
        }
    }

    public ServerConnection PrepareBidirectionalConnection()
    {
        if (this.BidirectionalConnection is { } connection)
        {
            return connection;
        }
        else
        {
            return this.ConnectionTerminal.PrepareBidirectionalConnection(this);
        }
    }

    public bool Equals(ClientConnection? other)
    {
        if (other is null)
        {
            return false;
        }

        return this.ConnectionId == other.ConnectionId;
    }

    public override int GetHashCode()
        => (int)this.ConnectionId;

    public int CompareTo(ClientConnection? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (this.ConnectionId < other.ConnectionId)
        {
            return -1;
        }
        else if (this.ConnectionId > other.ConnectionId)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementOpenCountInternal()
    {// lock (this.clientConnections.SyncObject)
        this.openCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int DecrementOpenCountInternal()
    {// lock (this.clientConnections.SyncObject)
        return this.openCount > 0 ? --this.openCount : 0;
    }
}
