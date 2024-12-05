// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class RelayNode
{
    [Link(Primary = true, Name = "LinkedList", Type = ChainType.LinkedList)]
    [Link(Name = "RelayId", TargetMember = "RelayId", Type = ChainType.Unordered)]
    public RelayNode(ushort relayId, ClientConnection clientConnection)
    {
        this.Endpoint = new(relayId, clientConnection.DestinationEndpoint.EndPoint);
        clientConnection.UnsafeCopyKey(this.Key);
        clientConnection.UnsafeCopyIv(this.Iv);
    }

    public ushort RelayId // For chain
        => this.Endpoint.RelayId;

    [Link(Type = ChainType.Unordered)]
    public NetEndpoint Endpoint { get; private set; }

    internal byte[] Key { get; private set; } = new byte[32];

    internal byte[] Iv { get; private set; } = new byte[16];

    public void Clear()
    {
        this.Key.AsSpan().Fill(0);
        this.Iv.AsSpan().Fill(0);
    }

    public override string ToString()
        => this.Endpoint.ToString();
}
