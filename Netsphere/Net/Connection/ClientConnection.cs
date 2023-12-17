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

    public async Task<NetResult> SendAsync<TSend>(TSend packet)
        where TSend : ITinyhandSerialize<TSend>
    {
        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return NetResult.SerializationError;
        }

        if (this.NetBase.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        using (var transmission = await this.CreateTransmission().ConfigureAwait(false))
        {
            if (transmission is null)
            {
                return NetResult.NoTransmission;
            }

            var responseTcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = transmission.SendBlock(0, 0, owner, responseTcs, false);
            if (result != NetResult.Success)
            {
                return result;
            }

            var response = await responseTcs.Task.ConfigureAwait(false);
            response.Return();
            return response.Result;
        }
    }

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend packet)
        where TSend : ITinyhandSerialize<TSend>
        where TReceive : ITinyhandSerialize<TReceive>
    {
        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return (NetResult.SerializationError, default);
        }

        if (this.NetBase.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        using (var transmission = await this.CreateTransmission().ConfigureAwait(false))
        {
            if (transmission is null)
            {
                return (NetResult.NoTransmission, default);
            }

            var responseTcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = transmission.SendBlock(0, 0, owner, responseTcs, true);
            if (result != NetResult.Success)
            {
                return (result, default);
            }

            var response = await responseTcs.Task.ConfigureAwait(false);
            if (response.IsFailure)
            {
                return (response.Result, default);
            }

            if (!BlockService.TryDeserialize<TReceive>(response.Received, out var receive))
            {
                response.Return();
                return (NetResult.DeserializationError, default);
            }

            response.Return();
            return (NetResult.Success, receive);
        }
    }

    public async Task<NetStream?> CreateStream(long size)
    {
        if (this.NetBase.CancellationToken.IsCancellationRequested)
        {
            return default;
        }
        else if (this.Agreement.MaxStreamSize < size)
        {
            return default;
        }

        var transmission = await this.CreateTransmission().ConfigureAwait(false);
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
