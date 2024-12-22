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
        this.ClientConnection.Dispose();
    }

    public RelayId RelayId => this.Endpoint.RelayId;

    public bool IsOpen => this.ClientConnection.IsOpen;

    [Link(Type = ChainType.Unordered)]
    public NetEndpoint Endpoint { get; private set; }

    public ClientConnection ClientConnection { get; }

    internal byte[] InnerKeyAndNonce { get; private set; } = [];

    public override string ToString()
        => this.Endpoint.ToString();

    internal void Remove()
    {// using (RelayCircuit.relayNodes.LockObject.EnterScope())
        if (this.Goshujin is not null)
        {
            this.Goshujin = null;

            if (this.IsOpen)
            {
            }
        }
    }
}
