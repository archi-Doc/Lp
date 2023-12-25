// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
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
    public ClientConnection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetEndPoint endPoint)
        : base(packetTerminal, connectionTerminal, connectionId, endPoint)
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

    public async Task<NetResult> Send<TSend>(TSend packet)
        where TSend : ITinyhandSerialize<TSend>
    {
        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return NetResult.SerializationError;
        }

        if (this.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        using (var transmission = await this.TryCreateSendTransmission().ConfigureAwait(false))
        {
            if (transmission is null)
            {
                return NetResult.NoTransmission;
            }

            var tcs = new TaskCompletionSource<NetResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = transmission.SendBlock(0, 0, owner, tcs);
            if (result != NetResult.Success)
            {
                return result;
            }

            try
            {
                result = await tcs.Task.WaitAsync(this.CancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return NetResult.Canceled;
            }

            return result;
        }
    }

    public async Task<NetResultValue<TReceive>> SendAndReceive<TSend, TReceive>(TSend packet, ulong dataId = 0)
        where TSend : ITinyhandSerialize<TSend>
        where TReceive : ITinyhandSerialize<TReceive>
    {
        if (this.IsClosedOrDisposed)
        {
            return new(NetResult.Closed);
        }

        if (this.CancellationToken.IsCancellationRequested)
        {
            return new(NetResult.Canceled);
        }

        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return new(NetResult.SerializationError);
        }

        NetResponse response;
        using (var sendTransmission = await this.TryCreateSendTransmission().ConfigureAwait(false))
        {
            if (sendTransmission is null)
            {
                owner.Return();
                return new(NetResult.NoTransmission);
            }

            var result = sendTransmission.SendBlock(0, dataId, owner, default);
            owner.Return();
            if (result != NetResult.Success)
            {
                return new(result);
            }

            var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var receiveTransmission = this.TryCreateReceiveTransmission(sendTransmission.TransmissionId, tcs, default))
            {
                if (receiveTransmission is null)
                {
                    return new(NetResult.NoTransmission);
                }

                try
                {
                    response = await tcs.Task.WaitAsync(this.CancellationToken).ConfigureAwait(false);
                    if (response.IsFailure)
                    {
                        return new(response.Result);
                    }
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

    public async Task<ReceiveStreamResult> SendAndReceiveStream<TSend>(TSend packet, ulong dataId = 0)
        where TSend : ITinyhandSerialize<TSend>
    {
        if (this.CancellationToken.IsCancellationRequested)
        {
            return new(NetResult.Canceled);
        }

        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return new(NetResult.SerializationError);
        }

        using (var sendTransmission = await this.TryCreateSendTransmission().ConfigureAwait(false))
        {
            if (sendTransmission is null)
            {
                owner.Return();
                return new(NetResult.NoTransmission);
            }

            var result = sendTransmission.SendBlock(0, dataId, owner, default);
            owner.Return();
            if (result != NetResult.Success)
            {
                // stream.Dispose();
                return new(result);
            }

            var stream = new ReceiveStream();

            return new(NetResult.Success, stream);
        }
    }

    public async Task<NetStream?> CreateStream(long size)
    {
        if (this.CancellationToken.IsCancellationRequested)
        {
            return default;
        }
        else if (this.Agreement.MaxStreamSize < size)
        {
            return default;
        }

        var transmission = await this.TryCreateSendTransmission().ConfigureAwait(false);
        if (transmission is null)
        {
            return default;
        }

        var result = transmission.SendStream(0, 0, size, false);
        if (result != NetResult.Success)
        {
            transmission.Dispose();
            return default;
        }

        return default;
        // return transmission;
    }
}
