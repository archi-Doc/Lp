// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401

namespace Netsphere;

/// <summary>
/// Initializes a new instance of the <see cref="NetTerminalGene"/> class.
/// </summary>
internal class NetTerminalGene
{// NetTerminalGene by Nihei.
    public enum State
    {
        // NetTerminalGeneState:
        // Send: Initial -> SetSend() -> WaitingToSend -> (Send) -> WaitingForAck -> (Receive Ack) -> SendComplete.
        // Receive: Initial -> SetReceive() -> WaitingToReceive -> (Receive) -> (Managed: SendingAck) -> (Send Ack) -> ReceiveComplete.
        Initial,
        WaitingToSend,
        WaitingForAck,
        SendComplete,
        WaitingToReceive,
        SendingAck,
        ReceiveComplete,
    }

    public NetTerminalGene(ulong gene, NetInterface netInterface)
    {
        this.Gene = gene;
        this.NetInterface = netInterface;
    }

    public NetInterface NetInterface { get; }

    public State GeneState { get; internal set; }

    public ulong Gene { get; private set; }

    public PacketId ReceivedId { get; private set; }

    /// <summary>
    ///  Gets the packet (header + data) to send or the received data.
    /// </summary>
    public ByteArrayPool.MemoryOwner Owner { get; private set; }

    internal long SentMics;

    public bool IsAvailable
        => this.GeneState == State.Initial ||
        this.GeneState == State.SendComplete ||
        this.GeneState == State.ReceiveComplete;

    public bool IsComplete
        => this.GeneState == State.SendComplete || this.GeneState == State.ReceiveComplete;

    public bool IsSend
        => this.GeneState == State.WaitingToSend ||
        this.GeneState == State.WaitingForAck ||
        this.GeneState == State.SendComplete;

    public bool IsSendComplete
        => this.GeneState == State.SendComplete;

    public bool IsReceive
        => this.GeneState == State.WaitingToReceive ||
        this.GeneState == State.SendingAck ||
        this.GeneState == State.ReceiveComplete;

    public bool IsReceiveComplete
        => this.GeneState == State.SendingAck || this.GeneState == State.ReceiveComplete;

    public bool SetSend(ByteArrayPool.MemoryOwner toBeMoved)
    {
        if (this.IsAvailable)
        {
            this.GeneState = State.WaitingToSend;
            this.Owner.Owner?.Return();

            if (this.NetInterface.NetTerminalObsolete.TryEncryptPacket(toBeMoved, this.Gene, out var owner2))
            {// Encrypt
                toBeMoved.Return();
                this.Owner = owner2;
            }
            else
            {// No encrypt
                this.Owner = toBeMoved;
            }

            this.NetInterface.Terminal.AddInbound(this);
            return true;
        }

        return false;
    }

    public bool SetReceive()
    {
        if (this.IsAvailable)
        {
            this.GeneState = State.WaitingToReceive;
            this.Owner = this.Owner.Return();

            this.NetInterface.Terminal.AddInbound(this);
            return true;
        }

        return false;
    }

    public bool Send()
    {
        if (this.GeneState == State.WaitingToSend ||
            this.GeneState == State.WaitingForAck)
        {
            var currentCapacity = Interlocked.Decrement(ref this.NetInterface.Terminal.SendCapacityPerRound);
            if (currentCapacity < 0)
            {
                return false;
            }

            /*if (RandomVault.Pseudo.NextDouble() < 0.1)
            {
                this.State = NetTerminalGeneState.WaitingForAck;
                return true;
            }*/

            this.NetInterface.Terminal.TrySend(this.Owner.Memory.Span, this.NetInterface.NetTerminalObsolete.Endpoint.EndPoint);

            this.GeneState = State.WaitingForAck;

            if (this.NetInterface.NetTerminalObsolete.Logger is { } logger)
            {
                var span = this.Owner.Memory.Span;
                if (span.Length > 4)
                {
                    var packetId = (PacketId)span[3];
                    logger.Log($"Udp Send({currentCapacity}, {this.Gene.To4Hex()}) Id: {packetId}, Size: {span.Length}, To: {this.NetInterface.NetTerminalObsolete.Endpoint}");
                }
            }

            return true;
        }

        return false;
    }

    public bool ReceiveAck(long currentMics)
    {
        /*if (RandomVault.Pseudo.NextDouble() < 0.5)
        {
            this.NetInterface.NetTerminalObsolete.Logger?.Log($"Ack cancel: {this.Gene.To4Hex()}");
            return false;
        }*/

        this.NetInterface.NetTerminalObsolete.Logger?.Log($"ReceiveAck({this.Gene.To4Hex()})");

        if (this.GeneState == State.WaitingForAck)
        {
            this.NetInterface.NetTerminalObsolete.FlowControl.ReportAck(currentMics, this.SentMics);
            this.GeneState = State.SendComplete;
            return true;
        }

        return false;
    }

    public bool Receive(PacketId id, ByteArrayPool.MemoryOwner owner, long currentMics)
    {// lock (this.NetTerminalObsolete.SyncObject)
        if (this.GeneState == State.WaitingToReceive)
        {// Receive data
            this.ReceivedId = id;
            this.Owner.Owner?.Return();

            if (this.NetInterface.NetTerminalObsolete.TryDecryptPacket(owner, this.Gene, out var owner2))
            {// Decrypt
                this.Owner = owner2;
            }
            else
            {
                this.Owner = owner.IncrementAndShare();
            }

            if (this.NetInterface.NetTerminalObsolete.Logger is { } logger)
            {
                var span = this.Owner.Memory.Span;
                if (span.Length > 4)
                {
                    var packetId = (PacketId)span[3];
                    logger.Log($"Receive({this.Gene.To4Hex()}) Id: {this.ReceivedId}, Size: {span.Length}, To: {this.NetInterface.NetTerminalObsolete.Endpoint}");
                }
            }

            SendAck();

            return true;
        }
        else if (this.GeneState == State.SendingAck)
        {// Already received.
            return true;
        }
        else if (this.GeneState == State.ReceiveComplete)
        {// Resend Ack
            SendAck();
            return true;
        }

        return false;

        void SendAck()
        {
            if (!this.NetInterface.NetTerminalObsolete.IsEncrypted && PacketService.IsManualAck(this.ReceivedId))
            {
                this.GeneState = State.ReceiveComplete;
            }
            else
            {
                if (this.NetInterface.RecvGenes?.Length == 1)
                {
                    // this.NetInterface.NetTerminalObsolete.Logger?.Log($"SendAck {this.Gene.To4Hex()}");
                    this.NetInterface.NetTerminalObsolete.SendAck(this.Gene);
                    this.GeneState = State.ReceiveComplete;
                }
                else
                {
                    // this.NetInterface.NetTerminalObsolete.Logger?.Log($"SendingAck {this.Gene.To4Hex()}");
                    this.GeneState = State.SendingAck;
                }
            }
        }
    }

    public override string ToString()
    {
        var length = this.Owner.Memory.Length;
        if (this.IsSend && length >= PacketService.HeaderSize)
        {
            length -= PacketService.HeaderSize;
        }

        return $"{this.Gene.To4Hex()}, {this.GeneState}, Data: {length}";
    }

    internal void Clear()
    {// lock (this.NetTerminalObsolete.SyncObject)
        /*if (this.State == NetTerminalGeneState.SendingAck || this.State == NetTerminalGeneState.ReceiveComplete)
        {// (this.State == NetTerminalGeneState.WaitingForAck)
        }*/

        this.NetInterface.Terminal.RemoveInbound(this);
        this.GeneState = State.Initial;
        this.Gene = 0;
        this.ReceivedId = PacketId.Invalid;
        this.Owner = this.Owner.Return();
    }
}
