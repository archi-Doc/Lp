// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class ServerConnection : Connection
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId")]
    [Link(Type = ChainType.Unordered, Name = "DestinationEndPoint", TargetMember = "DestinationEndPoint")]
    internal ServerConnection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetNode node, NetEndPoint endPoint)
        : base(packetTerminal, connectionTerminal, connectionId, node, endPoint)
    {
        this.context = this.NetBase.NewServerConnectionContext(this);
    }

    internal ServerConnection(ClientConnection clientConnection)
        : base(clientConnection)
    {
        this.context = this.NetBase.NewServerConnectionContext(this);
        this.BidirectionalConnection = clientConnection;
    }

    #region FieldAndProperty

    public override bool IsClient => false;

    public override bool IsServer => true;

    public ClientConnection? BidirectionalConnection { get; internal set; } // lock (this.ConnectionTerminal.clientConnections.SyncObject)

    private ServerConnectionContext context;

    #endregion

    public ServerConnectionContext GetContext()
        => this.context;

    public TContext GetContext<TContext>()
        where TContext : ServerConnectionContext
        => (TContext)this.context;

    public ClientConnection PrepareBidirectionalConnection()
    {
        if (this.BidirectionalConnection is { } connection)
        {
            return connection;
        }
        else
        {
            return this.ConnectionTerminal.PrepareBidirectionalConnection(this);
        }
    }
}
