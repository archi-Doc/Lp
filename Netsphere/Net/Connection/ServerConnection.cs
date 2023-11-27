// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class ServerConnection : Connection
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId", AddValue = false)]
    [Link(Type = ChainType.Unordered, Name = "OpenEndPoint", TargetMember = "EndPoint", AddValue = false, AutoLink = false)]
    [Link(Type = ChainType.Unordered, Name = "ClosedEndPoint", TargetMember = "EndPoint", AddValue = false, AutoLink = false)]
    [Link(Type = ChainType.LinkedList, Name = "OpenList", AutoLink = false)] // ResponseSystemMics
    [Link(Type = ChainType.LinkedList, Name = "ClosedList", AutoLink = false)] // ClosedSystemMics
    [Link(Type = ChainType.QueueList, Name = "SendQueue", AutoLink = false)]
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
}
