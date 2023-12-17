// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Netsphere.Net;

[ValueLinkObject(Restricted = true)]
internal partial class SendGene
{// lock (transmission.syncObject)
    [Link(Primary = true, Type = ChainType.SlidingList, Name = "GeneSerialList")]
    public SendGene(NetTransmission transmission)
    {
        this.Transmission = transmission;
        this.FlowControl = transmission.Connection.FlowControl; // Keep FlowControl instance as a member variable, since it may be subject to change.
    }

    #region FieldAndProperty

    public NetTransmission Transmission { get; }

    public FlowControl FlowControl { get; }

    public ByteArrayPool.MemoryOwner Packet { get; private set; }

    public long SentMics { get; private set; }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSend(ByteArrayPool.MemoryOwner toBeMoved)
    {
        this.Packet = toBeMoved;
        this.FlowControl.AddSend_LockFree(this); // Lock-free
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Send_NotThreadSafe(NetSender netSender)
    {
        var connection = this.Transmission.Connection;
        var currentMics = netSender.CurrentSystemMics;

        netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, this.Packet);
        this.SentMics = currentMics;
        return currentMics + connection.RetransmissionTimeout;
    }

    public void Dispose()
    {
        this.Packet = this.Packet.Return();
    }
}
