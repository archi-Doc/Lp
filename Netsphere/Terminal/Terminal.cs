// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;

namespace LP.Net;

public class Terminal
{
    internal struct UnregisteredSend
    {
        public UnregisteredSend(IPEndPoint endPoint, byte[] data)
        {
            this.EndPoint = endPoint;
            this.Data = data;
        }

        public IPEndPoint EndPoint { get; }

        public byte[] Data { get; }
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
    }

    internal void ProcessSend(UdpClient udp, long currentTicks)
    {
        while (this.unregisteredSends.TryDequeue(out var unregisteredSend))
        {
            udp.Send(unregisteredSend.Data, unregisteredSend.EndPoint);
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

    internal unsafe void ProcessReceive(IPEndPoint endPoint, byte[] data)
    {
        if (data.Length < PacketHelper.HeaderSize)
        {
            return;
        }

        PacketHeader header;
        fixed (byte* pb = data)
        {
            header = *(PacketHeader*)pb;
        }

        if (header.Engagement != 0)
        {
        }

        /*if (header.Id == PacketId.Punch)
        {
            var r = new PacketPunchResponse();
            r.Header = header;
            r.Header.Id = PacketId.PunchResponse;
            r.EndPoint = endPoint;
            r.UtcTicks = DateTime.UtcNow.Ticks;

            var b = TinyhandSerializer.Serialize(r);
            this.unregisteredSends.Enqueue(new UnregisteredSend(endPoint, b));
            return;
        }*/

        if (this.recvGenes.TryGetValue(header.Gene, out var terminalGene) && terminalGene.State != NetTerminalGene.State.Unmanaged)
        {
            terminalGene.NetTerminal.ProcessRecv(terminalGene, endPoint, ref header, data);
        }
        else
        {
            this.ProcessUnmanagedRecv(endPoint, ref header, data);
        }
    }

    internal void ProcessUnmanagedRecv(IPEndPoint endPoint, ref PacketHeader header, byte[] data)
    {
        if (header.Id == PacketId.Punch)
        {
            var r = new PacketPunchResponse();
            r.Header = header;
            r.EndPoint = endPoint;
            r.UtcTicks = DateTime.UtcNow.Ticks;

            var b = TinyhandSerializer.Serialize(r);
            this.unregisteredSends.Enqueue(new UnregisteredSend(endPoint, b));
        }
    }

    internal void AddRecvGene(NetTerminalGene[] recvGenes)
    {
        foreach (var x in recvGenes)
        {
            this.recvGenes.TryAdd(x.Gene, x);
        }
    }

    private NetTerminal.GoshujinClass terminals = new();

    private ConcurrentDictionary<ulong, NetTerminalGene> recvGenes = new();

    private ConcurrentQueue<UnregisteredSend> unregisteredSends = new();
}
