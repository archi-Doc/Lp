// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class RelayNode
{
    public RelayNode(ushort relayId, NetNode netNode)
    {
        this.RelayId = relayId;
        this.NetNode = netNode;
    }

    [Link(Primary = true, Type = ChainType.Unordered)]
    public ushort RelayId { get; private set; }

    [Link(Type = ChainType.Unordered)]
    public NetNode NetNode { get; private set; }
}
