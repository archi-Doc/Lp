// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class ClientConnection : NetConnection
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId", AddValue = false)]
    [Link(Type = ChainType.Unordered, Name = "OpenEndPoint", TargetMember = "EndPoint", AddValue = false, AutoLink = false)]
    [Link(Type = ChainType.Unordered, Name = "ClosedEndPoint", TargetMember = "EndPoint", AddValue = false, AutoLink = false)]
    [Link(Type = ChainType.LinkedList, Name = "ClosedList", AutoLink = false)]
    public ClientConnection(ulong connectionId, NetEndPoint endPoint)
        : base(connectionId, endPoint)
    {
    }

    // public async Task<(NetResult Result, ulong DataId, ByteArrayPool.MemoryOwner Value)> SendAndReceiveServiceAsync(ulong dataId, ByteArrayPool.MemoryOwner data)
}
