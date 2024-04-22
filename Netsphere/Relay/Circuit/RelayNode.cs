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
        clientConnection.UnsafeCopyKey(this.Key);
        clientConnection.UnsafeCopyIv(this.Iv);
    }

    [Link(Type = ChainType.Unordered)]
    public ushort RelayId { get; private set; }

    [Link(Type = ChainType.Unordered)]
    public NetNode NetNode { get; private set; }

    internal byte[] Key { get; private set; } = new byte[Connection.EmbryoKeyLength];

    internal byte[] Iv { get; private set; } = new byte[Connection.EmbryoIvLength];
}
