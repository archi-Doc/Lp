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
    // [Link(Type = ChainType.QueueList, Name = "SendQueue", AutoLink = false)]
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

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
        where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return (NetResult.SerializationError, default);
        }

        if (this.NetBase.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        var transmission = await this.GetTransmission().ConfigureAwait(false);
        if (transmission is null)
        {
            return (NetResult.NoNetwork, default);
        }

        transmission.SendBlock((uint)TSend.PacketType, 0, owner);

        NetResponseData response = await transmission.ReceiveBlock().ConfigureAwait(false);
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
