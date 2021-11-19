// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using Arc.Threading;

#pragma warning disable SA1401

namespace LP.Net;

internal static class NetTerminalGeneExtension
{
    public static bool IsUnavailable(this NetTerminalGene[] genes)
    {
        foreach (var gene in genes)
        {
            if (!gene.IsAvailable)
            {
                return false;
            }
        }

        return true;
    }
}

internal enum NetTerminalGeneState
{
    // NetTerminalGeneState:
    // Send: Initial -> SetSend() -> WaitingToSend -> (Send) -> WaitingForAck -> (Receive Ack) -> Complete.
    // Receive: Initial -> SetReceive() -> WaitingToReceive -> (Receive) -> (Managed: SendingAck) -> (Send Ack) -> Complete.
    Initial,
    WaitingToSend,
    WaitingForAck,
    WaitingToReceive,
    SendingAck,
    Complete,
}

/// <summary>
/// Initializes a new instance of the <see cref="NetTerminalGene"/> class.
/// </summary>
internal class NetTerminalGene// : IEquatable<NetTerminalGene>
{
    public NetTerminalGene(ulong gene, NetTerminal netTerminal)
    {
        this.Gene = gene;
        this.NetTerminal = netTerminal;
    }

    public bool IsAvailable => this.State == NetTerminalGeneState.Initial || this.State == NetTerminalGeneState.Complete;

    public bool IsComplete => this.State == NetTerminalGeneState.Complete;

    public bool SetSend(byte[] packet)
    {
        if (this.IsAvailable)
        {
            this.State = NetTerminalGeneState.WaitingToSend;
            this.packetToSend = packet;

            // var packetId = (PacketId)packet[1];
            // Logger.Default.Debug($"SetSend: {packetId} -> {this.PacketId}, {this.State}");
            return true;
        }

        return false;
    }

    public bool SetReceive()
    {
        if (this.IsAvailable)
        {
            this.State = NetTerminalGeneState.WaitingToReceive;
            this.ReceivedData = default;
            return true;
        }

        return false;
    }

    public bool Send(UdpClient udp)
    {
        if (this.State == NetTerminalGeneState.WaitingToSend ||
            this.State == NetTerminalGeneState.WaitingForAck)
        {
            if (this.packetToSend == null)
            {// Error
                this.State = NetTerminalGeneState.Initial;
            }
            else
            {
                udp.Send(this.packetToSend, this.NetTerminal.Endpoint);
                this.State = NetTerminalGeneState.WaitingForAck;

                // var packetId = (PacketId)packetToSend[1];
                // Logger.Default.Debug($"Send: {packetId}, {this.NetTerminal.Endpoint}");
                return true;
            }
        }

        return false;
    }

    public bool ReceiveAck()
    {
        if (this.State == NetTerminalGeneState.WaitingForAck)
        {
            this.State = NetTerminalGeneState.Complete;
            return true;
        }

        return false;
    }

    public bool Receive(Memory<byte> data)
    {
        if (this.State == NetTerminalGeneState.WaitingToReceive)
        {// Receive data
            this.State = NetTerminalGeneState.SendingAck;
            this.ReceivedData = data;

            // Logger.Default.Debug($"Receive: {this.PacketId}, {this.NetTerminal.Endpoint}");
            return true;
        }
        else if (this.State == NetTerminalGeneState.SendingAck)
        {// Already received.
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

    public override string ToString()
    {
        var sendData = this.packetToSend == null ? 0 : this.packetToSend.Length;
        return $"{this.Gene:x4} {this.State}, SendData: {sendData}, RecvData: {this.ReceivedData.Length}";
    }

    public NetTerminal NetTerminal { get; }

    public NetTerminalGeneState State { get; private set; }

    public ulong Gene { get; private set; }

    /// <summary>
    ///  Gets the received data.
    /// </summary>
    public Memory<byte> ReceivedData { get; private set; }

    /// <summary>
    ///  The byte array (header + data) to send.
    /// </summary>
    private byte[]? packetToSend;

#pragma warning disable SA1202 // Elements should be ordered by access
    internal long SentTicks;
#pragma warning restore SA1202 // Elements should be ordered by access
}
