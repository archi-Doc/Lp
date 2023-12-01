// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;
using Netsphere.Packet;
using Netsphere.Transmission;

namespace Netsphere;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class ServerConnection : Connection
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId", AddValue = false, Accessibility = ValueLinkAccessibility.Private)]
    [Link(Type = ChainType.Unordered, Name = "OpenEndPoint", TargetMember = "EndPoint", AddValue = false, AutoLink = false, Accessibility = ValueLinkAccessibility.Private)]
    [Link(Type = ChainType.Unordered, Name = "ClosedEndPoint", TargetMember = "EndPoint", AddValue = false, AutoLink = false, Accessibility = ValueLinkAccessibility.Private)]
    [Link(Type = ChainType.LinkedList, Name = "OpenList", AutoLink = false, Accessibility = ValueLinkAccessibility.Private)] // ResponseSystemMics
    [Link(Type = ChainType.LinkedList, Name = "ClosedList", AutoLink = false, Accessibility = ValueLinkAccessibility.Private)] // ClosedSystemMics
    [Link(Type = ChainType.QueueList, Name = "SendQueue", AutoLink = false, Accessibility = ValueLinkAccessibility.Private)]
    public ServerConnection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetEndPoint endPoint, ConnectionAgreementBlock agreement)
        : base(packetTerminal, connectionTerminal, connectionId, endPoint, agreement)
    {
    }

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

    internal override void UpdateSendQueue(SendTransmission transmission)
    {
        lock (this.sendTransmissions.SyncObject)
        {
            this.sendTransmissions.SendQueueChain.TryEnqueue(transmission);
        }
    }
}
