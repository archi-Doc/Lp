// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Netsphere.Net;

[ValueLinkObject(Restricted = true)]
internal partial class NetGene : IDisposable
{// lock (transmission.syncObject)
    public enum GeneState
    {
        // GeneState:
        // Send: Initial -> SetSend() -> Sending -> Send()/Resend() -> (Receive Ack) -> Complete
        // Receive: Initial -> SetReceive() -> Received -> (Send Ack) -> Complete
        Initial,
        Sending,
        Received,
        Complete,
    }

    [Link(Primary = true, Type = ChainType.SlidingList, Name = "GenePositionList")]
    public NetGene(NetTransmission transmission)
    {
        this.Transmission = transmission;
        this.FlowControl = transmission.FlowControl; // Keep FlowControl instance as a member variable, since it may be subject to change.
    }

    #region FieldAndProperty

    public NetTransmission Transmission { get; }

    public FlowControl FlowControl { get; }

    public GeneState State { get; private set; }

    // public int GenePosition => this.SlidingListLink.Position;

    public ByteArrayPool.MemoryOwner Packet { get; private set; }

    public long SentMics { get; private set; }

    public bool IsReceived => this.State == GeneState.Received;

    public bool IsComplete => this.State == GeneState.Complete;

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSend(ByteArrayPool.MemoryOwner toBeMoved)
    {
        Debug.Assert(this.State == GeneState.Initial);
        this.State = GeneState.Sending;
        this.Packet = toBeMoved;

        this.FlowControl.AddSend_LockFree(this); // Lock-free
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRecv(ByteArrayPool.MemoryOwner toBeShared)
    {
        if (this.State == GeneState.Initial)
        {
            this.State = GeneState.Received;
            this.Packet = toBeShared.IncrementAndShare();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAck()
    {
        if (this.State == GeneState.Sending)
        {
            this.State = GeneState.Complete;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Send_NotThreadSafe(NetSender netSender)
    {
        if (this.State == GeneState.Sending)
        {
            var connection = this.Transmission.Connection;
            var currentMics = netSender.CurrentSystemMics;
            var rto = connection.RetransmissionTimeout;

            netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, this.Packet);
            this.SentMics = currentMics;
            return currentMics + connection.RetransmissionTimeout;
        }
        else
        {
            return 0;
        }
    }

    public void Dispose()
    {
        this.State = GeneState.Initial;
        this.Packet = this.Packet.Return();
        // this.FlowControl.Remove(this);
    }
}
