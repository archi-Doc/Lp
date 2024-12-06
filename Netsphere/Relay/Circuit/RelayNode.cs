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
        clientConnection.EmbryoKey.CopyTo(this.EmbryoKey);
        this.EmbryoSalt = clientConnection.EmbryoSalt;
        this.EmbryoSecret = clientConnection.EmbryoSecret;
    }

    public ushort RelayId // For chain
        => this.Endpoint.RelayId;

    [Link(Type = ChainType.Unordered)]
    public NetEndpoint Endpoint { get; private set; }

    internal byte[] EmbryoKey { get; private set; } = new byte[32];

    internal ulong EmbryoSalt { get; private set; }

    internal ulong EmbryoSecret { get; private set; }

    public void Clear()
    {
        this.EmbryoKey.AsSpan().Fill(0);
        this.EmbryoSalt = 0;
        this.EmbryoSecret = 0;
    }

    public override string ToString()
        => this.Endpoint.ToString();
}
