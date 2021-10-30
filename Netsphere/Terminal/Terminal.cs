﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;

namespace LP.Net;

public class Terminal
{
    private const int InitialBufferLength = 2048;

    internal struct UnmanagedSend
    {
        public UnmanagedSend(IPEndPoint endPoint, byte[] packet)
        {
            this.EndPoint = endPoint;
            this.Packet = packet;
        }

        public IPEndPoint EndPoint { get; }

        public byte[] Packet { get; }
    }

    /// <summary>
    /// Create raw (without public key) NetTerminal instance.
    /// </summary>
    /// <param name="nodeAddress">NodeAddress.</param>
    /// <returns>NetTerminal.</returns>
    public NetTerminal Create(NodeAddress nodeAddress)
    {
        var gene = Random.Crypto.NextULong();
        var terminal = new NetTerminal(this, gene, nodeAddress);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        return terminal;
    }

    public Terminal()
    {
        Radio.Open<Message.Start>(this.Start);
        Radio.Open<Message.Stop>(this.Stop);
    }

    public void Start(Message.Start message)
    {
        this.Core = new ThreadCoreGroup(message.ParentCore);
    }

    public void Stop(Message.Stop message)
    {
        this.Core?.Dispose();
        this.Core = null;
    }

    public ThreadCoreBase? Core { get; private set; }

    internal void ProcessSend(UdpClient udp, long currentTicks)
    {
        while (this.unmanagedSends.TryDequeue(out var unregisteredSend))
        {
            udp.Send(unregisteredSend.Packet, unregisteredSend.EndPoint);
        }

        NetTerminal[] array;
        lock (this.terminals)
        {
            array = this.terminals.QueueChain.ToArray();
        }

        foreach (var x in array)
        {
            x.ProcessSend(udp, currentTicks);
        }
    }

    internal unsafe void ProcessReceive(IPEndPoint endPoint, byte[] packet)
    {
        var position = 0;
        var remaining = packet.Length;

        while (remaining >= PacketHelper.HeaderSize)
        {
            PacketHeader header;
            fixed (byte* pb = packet)
            {
                header = *(PacketHeader*)pb;
            }

            var dataSize = header.DataSize;
            if (remaining < (PacketHelper.HeaderSize + dataSize))
            {// Invalid DataSize
                return;
            }

            if (header.Engagement != 0)
            {
            }

            position += PacketHelper.HeaderSize;
            this.ProcessReceiveCore(endPoint, ref header, packet, position, dataSize);
            position += dataSize;
            remaining -= PacketHelper.HeaderSize + dataSize;
        }
    }

    internal void ProcessReceiveCore(IPEndPoint endPoint, ref PacketHeader header, byte[] packet, int dataPosition, int dataSize)
    {
        if (this.managedGenes.TryGetValue(header.Gene, out var terminalGene) && terminalGene.State != NetTerminalGeneState.Unmanaged)
        {
            var netTerminal = terminalGene.NetTerminal;
            if (!netTerminal.EndPoint.Equals(endPoint))
            {// EndPoint mismatch.
                return;
            }

            if (!terminalGene.NetTerminal.ProcessRecv(terminalGene, endPoint, ref header, packet))
            {
                this.ProcessUnmanagedRecv(endPoint, ref header, packet);
            }
        }
        else
        {
            this.ProcessUnmanagedRecv(endPoint, ref header, packet);
        }
    }

    internal unsafe void ProcessUnmanagedRecv(IPEndPoint endPoint, ref PacketHeader header, byte[] packet)
    {
        if (header.Id == PacketId.Punch)
        {// Punch
            var r = new PacketPunchResponse();
            r.EndPoint = endPoint;
            r.UtcTicks = DateTime.UtcNow.Ticks;

            header.Id = PacketId.PunchResponse;
            var p = this.CreatePacket(ref header, r);

            this.unmanagedSends.Enqueue(new UnmanagedSend(endPoint, p));
        }
        else
        {// Not supported
        }
    }

    internal unsafe byte[] CreatePacket<T>(ref PacketHeader header, T value)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferLength];
        }

        var writer = new Tinyhand.IO.TinyhandWriter(initialBuffer);
        var span = writer.GetSpan(PacketHelper.HeaderSize);
        writer.Advance(PacketHelper.HeaderSize);

        var written = writer.Written;
        TinyhandSerializer.Serialize(ref writer, value);
        fixed (byte* pb = span)
        {
            header.DataSize = (ushort)(writer.Written - written);
            *(PacketHeader*)pb = header;
        }

        return writer.FlushAndGetArray();
    }

    internal void AddNetTerminalGene(NetTerminalGene[] genes)
    {
        foreach (var x in genes)
        {
            if (x.State == NetTerminalGeneState.WaitingToSend ||
                x.State == NetTerminalGeneState.WaitingToReceive)
            {
                this.managedGenes.TryAdd(x.Gene, x);
            }
        }
    }

    [ThreadStatic]
    private static byte[]? initialBuffer;

    private NetTerminal.GoshujinClass terminals = new();

    private ConcurrentDictionary<ulong, NetTerminalGene> managedGenes = new();

    private ConcurrentQueue<UnmanagedSend> unmanagedSends = new();
}
