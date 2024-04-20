// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere.Relay;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class RelayNode
{
    [Link(Primary = true, Name = "List", Type = ChainType.List)]
    public RelayNode(ushort relayId, NetNode netNode)
    {
        this.RelayId = relayId;
        this.NetNode = netNode;
    }

    [Link(Type = ChainType.Unordered)]
    public ushort RelayId { get; private set; }

    [Link(Type = ChainType.Unordered)]
    public NetNode NetNode { get; private set; }

    public EncryptionPublicKey PublicKey { get; private set; }
}
