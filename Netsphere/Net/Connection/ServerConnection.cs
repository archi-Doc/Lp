// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;
using Netsphere.Server;

namespace Netsphere;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class ServerConnection : Connection
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId")]
    [Link(Type = ChainType.Unordered, Name = "OpenEndPoint", TargetMember = "DestinationEndPoint")]
    [Link(Type = ChainType.Unordered, Name = "ClosedEndPoint", TargetMember = "DestinationEndPoint")]
    [Link(Type = ChainType.LinkedList, Name = "OpenList", AutoLink = false)] // ResponseSystemMics
    [Link(Type = ChainType.LinkedList, Name = "ClosedList", AutoLink = false)] // ClosedSystemMics
    internal ServerConnection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetNode node, NetEndPoint endPoint)
        : base(packetTerminal, connectionTerminal, connectionId, node, endPoint)
    {
        this.context = this.NetBase.NewServerConnectionContext(this);
    }

    internal ServerConnection(ClientConnection clientConnection)
        : base(clientConnection)
    {
        this.context = this.NetBase.NewServerConnectionContext(this);
    }

    #region FieldAndProperty

    public override ConnectionState State
    {
        get
        {
            if (this.OpenEndPointLink.IsLinked)
            {
                return ConnectionState.Open;
            }
            else if (this.ClosedEndPointLink.IsLinked)
            {
                return ConnectionState.Closed;
            }
            else
            {
                return ConnectionState.Disposed;
            }
        }
    }

    public override bool IsClient => false;

    public override bool IsServer => true;

    private ServerConnectionContext context;

    #endregion

    public ServerConnectionContext GetContext()
        => this.context;

    public TContext GetContext<TContext>()
        where TContext : ServerConnectionContext
        => (TContext)this.context;

    public ClientConnection PrepareBidirectional()
        => this.ConnectionTerminal.PrepareBidirectional(this);
}
