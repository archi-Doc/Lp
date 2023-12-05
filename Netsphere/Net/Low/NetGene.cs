// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
internal partial class NetGene : IDisposable
{
    public enum GeneState
    {
        // GeneState:
        // Send: Initial -> SetSend() -> WaitingToSend -> Send() -> WaitingForAck -> (Receive Ack) -> Complete.
        // Receive: Initial -> SetReceive() -> WaitingToReceive -> (Receive) -> (Managed: SendingAck) -> (Send Ack) -> ReceiveComplete.
        Initial,
        WaitingToSend,
        WaitingForAck,
        WaitingToReceive,
        SendingAck,
        Complete,
    }

    [Link(Primary = true, Type = ChainType.SlidingList, Name = "SlidingList")]
    // [Link(Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false)]
    // [Link(Name = "ResendQueue", Type = ChainType.QueueList, AutoLink = false)]
    public NetGene()
    {
        // this.GenePosition = genePosition;
        // this.GeneMax = geneMax;
    }

    #region FieldAndProperty

    public GeneState State { get; private set; }

    public int GenePosition => this.SlidingListLink.Position;

    public ByteArrayPool.MemoryOwner Packet { get; private set; }

    public long SentMics { get; private set; }

    // public uint GeneMax { get; }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSend(ByteArrayPool.MemoryOwner toBeMoved)
    {
        Debug.Assert(this.State == GeneState.Initial);

        this.State = GeneState.WaitingToSend;
        this.Packet = toBeMoved;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(NetSender netSender, IPEndPoint endPoint, ref bool sentFlag)
    {
        if (this.State == GeneState.WaitingToSend ||
            this.State == GeneState.WaitingForAck)
        {
            netSender.Send_NotThreadSafe(endPoint, this.Packet);
            this.SentMics = netSender.CurrentSystemMics;
            sentFlag = true;
        }
    }

    public void Dispose()
    {
        this.State = GeneState.Initial;
        this.Packet = this.Packet.Return();
    }
}
