// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;
using Netsphere.Net;
using Netsphere.Packet;

namespace Netsphere;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class ClientConnection : Connection
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId")]
    [Link(Type = ChainType.Unordered, Name = "OpenEndPoint", TargetMember = "EndPoint")]
    [Link(Type = ChainType.Unordered, Name = "ClosedEndPoint", TargetMember = "EndPoint")]
    [Link(Type = ChainType.LinkedList, Name = "OpenList", AutoLink = false)] // ResponseSystemMics
    [Link(Type = ChainType.LinkedList, Name = "ClosedList", AutoLink = false)] // ClosedSystemMics
    public ClientConnection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetNode node, NetEndPoint endPoint)
        : base(packetTerminal, connectionTerminal, connectionId, node, endPoint)
    {
    }

    public override ConnectionState State
    {
        get
        {
            if (this.OpenEndPointLink.IsLinked)
            {
                return ConnectionState.Open;
            }
            else if (this.ClosedEndPointLink.IsLinked)
            {
                return ConnectionState.Closed;
            }
            else
            {
                return ConnectionState.Disposed;
            }
        }
    }

    public override bool IsClient => true;

    public override bool IsServer => false;

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

        if (!BlockService.TrySerialize(data, out var owner))
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

        dataId = dataId != 0 ? dataId : BlockService.GetId<TSend, TReceive>();
        if (!BlockService.TrySerialize(data, out var owner))
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

        if (!BlockService.TryDeserialize<TReceive>(response.Received, out var receive))
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

        if (!BlockService.TrySerialize(packet, out var owner))
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

    public async Task<NetResult> RequestAgreement(ConnectionAgreementBlock agreement)
    {
        var result = await this.SendAndReceive<ConnectionAgreementBlock, ConnectionAgreementBlock>(agreement, ConnectionAgreementBlock.DataId).ConfigureAwait(false);
        if (result.Result == NetResult.Success &&
            result.Value is not null)
        {
            this.Agreement.Update(result.Value);
        }

        return result.Result;
    }
}
