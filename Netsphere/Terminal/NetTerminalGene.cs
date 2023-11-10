﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401

namespace Netsphere;

internal enum NetTerminalGeneState
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

/// <summary>
/// Initializes a new instance of the <see cref="NetTerminalGene"/> class.
/// </summary>
internal class NetTerminalGene
{// NetTerminalGene by Nihei.
    public NetTerminalGene(ulong gene, NetInterface netInterface)
    {
        this.Gene = gene;
        this.NetInterface = netInterface;
    }

    public bool IsAvailable
        => this.State == NetTerminalGeneState.Initial ||
        this.State == NetTerminalGeneState.SendComplete ||
        this.State == NetTerminalGeneState.ReceiveComplete;

    public bool IsComplete
        => this.State == NetTerminalGeneState.SendComplete || this.State == NetTerminalGeneState.ReceiveComplete;

    public bool IsSend =>
        this.State == NetTerminalGeneState.WaitingToSend ||
        this.State == NetTerminalGeneState.WaitingForAck ||
        this.State == NetTerminalGeneState.SendComplete;

    public bool IsSendComplete => this.State == NetTerminalGeneState.SendComplete;

    public bool IsReceive =>
        this.State == NetTerminalGeneState.WaitingToReceive ||
        this.State == NetTerminalGeneState.SendingAck ||
        this.State == NetTerminalGeneState.ReceiveComplete;

    public bool IsReceiveComplete
        => this.State == NetTerminalGeneState.SendingAck || this.State == NetTerminalGeneState.ReceiveComplete;

    public bool SetSend(ByteArrayPool.MemoryOwner owner)
    {
        if (this.IsAvailable)
        {
            this.State = NetTerminalGeneState.WaitingToSend;
            this.Owner.Owner?.Return();

            if (this.NetInterface.NetTerminal.TryEncryptPacket(owner, this.Gene, out var owner2))
            {// Encrypt
                this.Owner = owner2;
            }
            else
            {
                this.Owner = owner.IncrementAndShare();
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
            this.State = NetTerminalGeneState.WaitingToReceive;
            this.Owner = this.Owner.Return();

            this.NetInterface.Terminal.AddInbound(this);
            return true;
        }

        return false;
    }

    public bool Send()
    {
        if (this.State == NetTerminalGeneState.WaitingToSend ||
            this.State == NetTerminalGeneState.WaitingForAck)
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

            try
            {
                this.NetInterface.Terminal.Send(this.Owner.Memory.Span, this.NetInterface.NetTerminal.Endpoint.EndPoint);
            }
            catch
            {
            }

            this.State = NetTerminalGeneState.WaitingForAck;

            if (this.NetInterface.NetTerminal.Logger is { } logger)
            {
                var span = this.Owner.Memory.Span;
                if (span.Length > 4)
                {
                    var packetId = (PacketId)span[3];
                    logger.Log($"Udp Send({currentCapacity}, {this.Gene.To4Hex()}) Id: {packetId}, Size: {span.Length}, To: {this.NetInterface.NetTerminal.Endpoint}");
                }
            }

            return true;
        }

        return false;
    }

    public bool ReceiveAck(long currentMics)
    {// lock (this.NetTerminal.SyncObject)
        /*if (RandomVault.Pseudo.NextDouble() < 0.5)
        {
            this.NetInterface.NetTerminal.Logger?.Log($"Ack cancel: {this.Gene.To4Hex()}");
            return false;
        }*/

        this.NetInterface.NetTerminal.Logger?.Log($"ReceiveAck({this.Gene.To4Hex()})");

        if (this.State == NetTerminalGeneState.WaitingForAck)
        {
            this.NetInterface.NetTerminal.FlowControl.ReportAck(currentMics, this.SentMics);
            this.State = NetTerminalGeneState.SendComplete;
            return true;
        }

        return false;
    }

    public bool Receive(PacketId id, ByteArrayPool.MemoryOwner owner, long currentMics)
    {// lock (this.NetTerminal.SyncObject)
        if (this.State == NetTerminalGeneState.WaitingToReceive)
        {// Receive data
            this.ReceivedId = id;
            this.Owner.Owner?.Return();

            if (this.NetInterface.NetTerminal.TryDecryptPacket(owner, this.Gene, out var owner2))
            {// Decrypt
                this.Owner = owner2;
            }
            else
            {
                this.Owner = owner.IncrementAndShare();
            }

            if (this.NetInterface.NetTerminal.Logger is { } logger)
            {
                var span = this.Owner.Memory.Span;
                if (span.Length > 4)
                {
                    var packetId = (PacketId)span[3];
                    logger.Log($"Receive({this.Gene.To4Hex()}) Id: {this.ReceivedId}, Size: {span.Length}, To: {this.NetInterface.NetTerminal.Endpoint}");
                }
            }

            SendAck();

            return true;
        }
        else if (this.State == NetTerminalGeneState.SendingAck)
        {// Already received.
            return true;
        }
        else if (this.State == NetTerminalGeneState.ReceiveComplete)
        {// Resend Ack
            SendAck();
            return true;
        }

        return false;

        void SendAck()
        {
            if (!this.NetInterface.NetTerminal.IsEncrypted && PacketService.IsManualAck(this.ReceivedId))
            {
                this.State = NetTerminalGeneState.ReceiveComplete;
            }
            else
            {
                if (this.NetInterface.RecvGenes?.Length == 1)
                {
                    // this.NetInterface.NetTerminal.Logger?.Log($"SendAck {this.Gene.To4Hex()}");
                    this.NetInterface.NetTerminal.SendAck(this.Gene);
                    this.State = NetTerminalGeneState.ReceiveComplete;
                }
                else
                {
                    // this.NetInterface.NetTerminal.Logger?.Log($"SendingAck {this.Gene.To4Hex()}");
                    this.State = NetTerminalGeneState.SendingAck;
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

        return $"{this.Gene.To4Hex()}, {this.State}, Data: {length}";
    }

    public NetInterface NetInterface { get; }

    public NetTerminalGeneState State { get; internal set; }

    public ulong Gene { get; private set; }

    public PacketId ReceivedId { get; private set; }

    /// <summary>
    ///  Gets the packet (header + data) to send or the received data.
    /// </summary>
    public ByteArrayPool.MemoryOwner Owner { get; private set; }

    internal void Clear()
    {// lock (this.NetTerminal.SyncObject)
        /*if (this.State == NetTerminalGeneState.SendingAck || this.State == NetTerminalGeneState.ReceiveComplete)
        {// (this.State == NetTerminalGeneState.WaitingForAck)
        }*/

        this.NetInterface.Terminal.RemoveInbound(this);
        this.State = NetTerminalGeneState.Initial;
        this.Gene = 0;
        this.ReceivedId = PacketId.Invalid;
        this.Owner = this.Owner.Return();
    }

#pragma warning disable SA1202 // Elements should be ordered by access
    internal long SentMics;
#pragma warning restore SA1202 // Elements should be ordered by access
}
