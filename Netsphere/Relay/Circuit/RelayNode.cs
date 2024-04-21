// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class RelayNode
{
    [Link(Primary = true, Name = "List", Type = ChainType.List)]
    public RelayNode(ushort relayId, ClientConnection clientConnection)
    {
        this.RelayId = relayId;
        this.NetNode = clientConnection.DestinationNode;
        this.embryo = clientConnection.UnsafeGetEmbryo();
    }

    [Link(Type = ChainType.Unordered)]
    public ushort RelayId { get; private set; }

    [Link(Type = ChainType.Unordered)]
    public NetNode NetNode { get; private set; }

    private Embryo embryo;
}
