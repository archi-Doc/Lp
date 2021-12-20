// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Arc.Threading;

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
internal class NetTerminalGene// : IEquatable<NetTerminalGene>
{
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

    public bool Send(UdpClient udp)
    {
        if (this.State == NetTerminalGeneState.WaitingToSend ||
            this.State == NetTerminalGeneState.WaitingForAck)
        {
            udp.Send(this.Owner.Memory.Span, this.NetInterface.NetTerminal.Endpoint);
            this.State = NetTerminalGeneState.WaitingForAck;

            // var packetId = (PacketId)packetToSend[1];
            // Logger.Default.Debug($"Send: {packetId}, {this.NetTerminal.Endpoint}");
            return true;
        }

        return false;
    }

    public bool ReceiveAck()
    {// lock (this.NetTerminal.SyncObject)
        /*if (LP.Random.Pseudo.NextDouble() < 0.99)
        {
            this.NetInterface.TerminalLogger?.Error($"Ack cancel: {this.Gene.To4Hex()}");
            return false;
        }*/

        if (this.State == NetTerminalGeneState.WaitingForAck)
        {
            this.State = NetTerminalGeneState.SendComplete;
            return true;
        }

        return false;
    }

    public bool Receive(PacketId id, ByteArrayPool.MemoryOwner owner)
    {// lock (this.NetTerminal.SyncObject)
        if (this.State == NetTerminalGeneState.WaitingToReceive)
        {// Receive data
            this.ReceivedId = id;
            this.Owner.Owner?.Return();

            if(this.NetInterface.NetTerminal.TryDecryptPacket(owner, this.Gene, out var owner2))
            {// Decrypt
                this.Owner = owner2;
            }
            else
            {
                this.Owner = owner.IncrementAndShare();
            }

            SendAck();

            // Logger.Default.Debug($"Receive: {this.PacketId}, {this.NetTerminal.Endpoint}");
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
                    this.NetInterface.TerminalLogger?.Information($"SendAck {this.Gene.To4Hex()}");
                    this.NetInterface.NetTerminal.SendAck(this.Gene);
                    this.State = NetTerminalGeneState.ReceiveComplete;
                }
                else
                {
                    this.NetInterface.TerminalLogger?.Information($"SendingAck {this.Gene.To4Hex()}");
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
        // this.NetInterface.TerminalLogger?.Information($"Clear: {this.State} - {this.Gene.To4Hex()}");

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
    internal long SentTicks;
#pragma warning restore SA1202 // Elements should be ordered by access
}
