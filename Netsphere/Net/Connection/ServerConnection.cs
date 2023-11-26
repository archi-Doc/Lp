// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[ValueLinkObject]
public partial class ServerConnection : ConnectionBase
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId", AddValue = false)]
    [Link(Type = ChainType.Unordered, Name = "OpenEndPoint", TargetMember = "EndPoint", AddValue = false, AutoLink = false)]
    [Link(Type = ChainType.Unordered, Name = "ClosedEndPoint", TargetMember = "EndPoint", AddValue = false, AutoLink = false)]
    [Link(Type = ChainType.LinkedList, Name = "ClosedList", AutoLink = false)]
    public ServerConnection(ConnectionTerminal connectionTerminal, ulong connectionId, NetEndPoint endPoint)
        : base(connectionTerminal, connectionId, endPoint)
    {
    }
}
