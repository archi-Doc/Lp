// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class RelayNode
{
    [Link(Primary = true, Name = "List", Type = ChainType.List)]
    [Link(Name = "RelayId", TargetMember = "RelayId", Type = ChainType.Unordered)]
    public RelayNode(ushort relayId, ClientConnection clientConnection)
    {
        // this.RelayId = relayId;
        this.Endpoint = new(clientConnection.DestinationEndpoint.EndPoint, relayId);
        clientConnection.UnsafeCopyKey(this.Key);
        clientConnection.UnsafeCopyIv(this.Iv);
    }

    // [Link(Type = ChainType.Unordered)]
    // public ushort RelayId { get; private set; }
    public ushort RelayId
        => this.Endpoint.RelayId;

    [Link(Type = ChainType.Unordered)]
    public NetEndpoint Endpoint { get; private set; }

    internal byte[] Key { get; private set; } = new byte[Connection.EmbryoKeyLength];

    internal byte[] Iv { get; private set; } = new byte[Connection.EmbryoIvLength];

    public void Clear()
    {
        this.Key.AsSpan().Fill(0);
        this.Iv.AsSpan().Fill(0);
    }
}
