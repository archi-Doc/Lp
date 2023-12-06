// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;
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
    public ClientConnection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetEndPoint endPoint, ConnectionAgreementBlock agreement)
        : base(packetTerminal, connectionTerminal, connectionId, endPoint, agreement)
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

        var transmission = await this.CreateTransmission().ConfigureAwait(false);
        if (transmission is null)
        {
            return NetResult.NoTransmission;
        }

        var tcs = new TaskCompletionSource<NetResult>();
        var result = transmission.SendBlock(0, 0, owner, tcs);
        if (result != NetResult.Success)
        {
            return result;
        }

        return await tcs.Task.ConfigureAwait(false);
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

        var transmission = await this.CreateTransmission().ConfigureAwait(false);
        if (transmission is null)
        {
            return (NetResult.NoTransmission, default);
        }

        var result = transmission.SendBlock(0, 0, owner, default);
        if (result != NetResult.Success)
        {
            return (result, default);
        }

        var response = await transmission.ReceiveBlock().ConfigureAwait(false);
        if (!response.IsSuccess)
        {
            return (response.Result, default);
        }

        if (!BlockService.TryDeserialize<TReceive>(response.Received, out var receive))
        {
            return (NetResult.DeserializationError, default);
        }

        return (NetResult.Success, receive);
    }
}
