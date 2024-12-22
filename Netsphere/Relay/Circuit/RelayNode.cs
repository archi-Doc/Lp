// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class RelayNode
{
    [Link(Primary = true, Name = "LinkedList", Type = ChainType.LinkedList)]
    [Link(Name = "RelayId", TargetMember = "RelayId", Type = ChainType.Unordered)]
    public RelayNode(RelayId relayId, byte[] innerKeyAndNonce, ClientConnection clientConnection)
    {
        this.Endpoint = new(relayId, clientConnection.DestinationEndpoint.EndPoint);
        this.ClientConnection = clientConnection;
        this.InnerKeyAndNonce = innerKeyAndNonce;
    }

    public RelayId RelayId // For chain
        => this.Endpoint.RelayId;

    [Link(Type = ChainType.Unordered)]
    public NetEndpoint Endpoint { get; private set; }

    public ClientConnection ClientConnection { get; }

    internal byte[] InnerKeyAndNonce { get; private set; } = [];

    public override string ToString()
        => this.Endpoint.ToString();
}
