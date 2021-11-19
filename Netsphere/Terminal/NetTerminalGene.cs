// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using Arc.Threading;

#pragma warning disable SA1401

namespace LP.Net;

internal enum NetTerminalGeneState
{
    // Send: Initial -> SetSend() -> WaitingToSend -> (Send) -> WaitingForAck -> (Ack) -> Complete.
    // Receive: Initial -> SetReceive() -> WaitingToReceive -> (Receive) -> Complete.
    Initial,
    WaitingToSend,
    WaitingForAck,
    WaitingToReceive,
    Complete,
}

/// <summary>
/// Initializes a new instance of the <see cref="NetTerminalGene"/> class.
/// </summary>
// [ValueLinkObject]
internal class NetTerminalGene// : IEquatable<NetTerminalGene>
{
    /*public static NetTerminalGene New()
    {
        return new NetTerminalGene(Random.Crypto.NextULong());
    }*/

    // [Link(Type = ChainType.QueueList, Name = "Queue", Primary = true)]
    public NetTerminalGene(ulong gene, NetTerminal netTerminal)
    {
        this.Gene = gene;
        this.NetTerminal = netTerminal;
    }

    public bool SetSend(byte[] packet)
    {
        if (this.State == NetTerminalGeneState.Initial ||
            this.State == NetTerminalGeneState.ReceivedOrConfirmed)
        {
            this.State = NetTerminalGeneState.WaitingToSend;
            this.packetToSend = packet;

            // var packetId = (PacketId)packet[1];
            // Logger.Default.Debug($"SetSend: {packetId} -> {this.PacketId}, {this.State}");
            return true;
        }

        return false;
    }

    public bool Send(UdpClient udp)
    {
        if (this.packetToSend == null)
        {
            return false;
        }

        if (this.State == NetTerminalGeneState.WaitingToSend)
        {
            udp.Send(this.packetToSend, this.NetTerminal.Endpoint);
            this.State = NetTerminalGeneState.WaitingForAck;

            Logger.Default.Debug($"Send: {this.PacketId}, {this.NetTerminal.Endpoint}");
            return true;
        }

        return false;
    }

    public bool Receive(Memory<byte> data)
    {
        if (this.State == NetTerminalGeneState.WaitingForAck ||
            this.State == NetTerminalGeneState.WaitingToReceive)
        {// Sent and waiting for confirmation, or waiting for the packet to arrive.
            /*if (!header.Id.IsResponse())
            {
                return false;
            }*/

            this.State = NetTerminalGeneState.ReceivedOrConfirmed;
            this.ReceivedData = data;

            Logger.Default.Debug($"Receive: {this.PacketId}, {this.NetTerminal.Endpoint}");
            return true;
        }

        return false;
    }

    public void Clear()
    {
        this.State = NetTerminalGeneState.Initial;
        this.Gene = 0;
        this.ReceivedData = default;
        this.packetToSend = null;
    }

    public NetTerminal NetTerminal { get; }

    public NetTerminalGeneState State { get; private set; }

    // [Link(Type = ChainType.Ordered)]
    public ulong Gene { get; private set; }

    /// <summary>
    /// Gets the PacketId of the packet.
    /// </summary>
    // public PacketId PacketId { get; private set; }

    /// <summary>
    ///  Gets the received data.
    /// </summary>
    public Memory<byte> ReceivedData { get; private set; }

    /*/// <summary>
    ///  Gets or sets the data of the packet.
    /// </summary>
    public Memory<byte>? Data { get; set; }*/

    public long InvokeTicks { get; set; }

    public long CompleteTicks { get; set; }

    /// <summary>
    ///  The byte array (header + data) to send.
    /// </summary>
    private byte[]? packetToSend;

    // public long CreatedTicks { get; } = Ticks.GetCurrent();

    /*public bool Equals(NetTerminalGene? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Gene == other.Gene;
    }*/
}
