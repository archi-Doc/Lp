// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Netsphere.Net;

[ValueLinkObject(Restricted = true)]
internal partial class NetGene : IDisposable
{// lock (transmission.syncObject)
    internal static readonly long ResendMics = Mics.FromMilliseconds(500);

    public enum GeneState
    {
        // GeneState:
        // Send: Initial -> SetSend() -> WaitingToSend -> Send() -> WaitingForAck -> (Receive Ack) -> Complete
        // Receive: Initial -> SetReceive() -> SendingAck -> Complete
        Initial,
        WaitingToSend,
        WaitingForAck,
        SendingAck,
        Complete,
    }

    [Link(Primary = true, Type = ChainType.SlidingList, Name = "SlidingList")]
    // [Link(Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false)]
    // [Link(Name = "ResendQueue", Type = ChainType.QueueList, AutoLink = false)]
    public NetGene()
    {
        // this.GenePosition = genePosition;
    }

    #region FieldAndProperty

    public GeneState State { get; private set; }

    // public int GenePosition => this.SlidingListLink.Position;

    public ByteArrayPool.MemoryOwner Packet { get; private set; }

    public long SentMics { get; private set; }

    public bool IsReceived => this.State == GeneState.SendingAck;

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSend(ByteArrayPool.MemoryOwner toBeMoved)
    {
        Debug.Assert(this.State == GeneState.Initial);

        this.State = GeneState.WaitingToSend;
        this.Packet = toBeMoved;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(NetSender netSender, IPEndPoint endPoint, ref int sentCount)
    {
        if (this.State == GeneState.WaitingToSend ||
            this.State == GeneState.WaitingForAck)
        {
            netSender.Send_NotThreadSafe(endPoint, this.Packet);
            this.SentMics = netSender.CurrentSystemMics;
            sentCount++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckResend(NetSender netSender)
    {
        if (this.State == GeneState.WaitingForAck)
        {
            if (netSender.CurrentSystemMics > this.SentMics + ResendMics)
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRecv(ByteArrayPool.MemoryOwner toBeShared)
    {
        Debug.Assert(this.State == GeneState.Initial);

        this.State = GeneState.SendingAck;
        this.Packet = toBeShared.IncrementAndShare();
    }

    public void Dispose()
    {
        this.State = GeneState.Initial;
        this.Packet = this.Packet.Return();
    }
}
