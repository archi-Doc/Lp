// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
internal partial class NetGene : IDisposable
{
    public enum GeneState
    {
        // GeneState:
        // Send: Initial -> SetSend() -> WaitingToSend -> (Send) -> WaitingForAck -> (Receive Ack) -> SendComplete.
        // Receive: Initial -> SetReceive() -> WaitingToReceive -> (Receive) -> (Managed: SendingAck) -> (Send Ack) -> ReceiveComplete.
        Initial,
        WaitingToSend,
        WaitingForAck,
        WaitingToReceive,
        SendingAck,
        Complete,
    }

    [Link(Primary = true, Type = ChainType.SlidingList, Name = "SlidingList", AddValue = false)]
    public NetGene(int genePosition)
    {
        this.GenePosition = genePosition;
        // this.GeneMax = geneMax;
    }

    #region FieldAndProperty

    public GeneState State { get; private set; }

    public int GenePosition { get; }

    public ByteArrayPool.MemoryOwner Packet { get; private set; }

    // public uint GeneMax { get; }

    #endregion

    public void SetSend(ByteArrayPool.MemoryOwner toBeMoved)
    {
        Debug.Assert(this.State == GeneState.Initial);

        this.State = GeneState.WaitingToSend;
        this.Packet = toBeMoved;
    }

    public void Dispose()
    {
        this.State = GeneState.Initial;
        this.Packet = this.Packet.Return();
    }
}
