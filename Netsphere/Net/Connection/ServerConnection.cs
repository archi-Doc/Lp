// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;
using Netsphere.Packet;

namespace Netsphere;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class ServerConnection : Connection
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId")]
    [Link(Type = ChainType.Unordered, Name = "OpenEndPoint", TargetMember = "EndPoint")]
    [Link(Type = ChainType.Unordered, Name = "ClosedEndPoint", TargetMember = "EndPoint")]
    [Link(Type = ChainType.LinkedList, Name = "OpenList", AutoLink = false)] // ResponseSystemMics
    [Link(Type = ChainType.LinkedList, Name = "ClosedList", AutoLink = false)] // ClosedSystemMics
    public ServerConnection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetEndPoint endPoint)
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

    /*public NetResult SendAndForget<TSend, TReceive>(TSend packet)
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

        var transmission = this.TryCreateTransmission();
        if (transmission is null)
        {
            return NetResult.NoTransmission;
        }

        var result = transmission.SendBlock(0, 0, owner, default);
        return result;
    }*/
}
