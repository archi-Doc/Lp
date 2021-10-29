// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;

namespace LP.Net;

public class Terminal
{
    internal struct UnmanagedSend
    {
        public UnmanagedSend(IPEndPoint endPoint, byte[] data)
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
        {// Below the minimum header size.
            return;
        }

        PacketHeader header;
        fixed (byte* pb = data)
        {
            header = *(PacketHeader*)pb;
        }

        if (data.Length != (PacketHelper.HeaderSize + header.DataSize))
        {// Invalid DataSize
            return;
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

        var span = data.AsSpan(PacketHelper.HeaderSize);
        if (this.managedGenes.TryGetValue(header.Gene, out var terminalGene) && terminalGene.State != NetTerminalGeneState.Unmanaged)
        {
            var netTerminal = terminalGene.NetTerminal;
            if (!netTerminal.EndPoint.Equals(endPoint))
            {// EndPoint mismatch.
                return;
            }

            if (!terminalGene.NetTerminal.ProcessRecv(terminalGene, endPoint, ref header, span))
            {
                this.ProcessUnmanagedRecv(endPoint, ref header, span);
            }
        }
        else
        {
            this.ProcessUnmanagedRecv(endPoint, ref header, span);
        }
    }

    internal unsafe void ProcessUnmanagedRecv(IPEndPoint endPoint, ref PacketHeader header, Span<byte> data)
    {
        if (header.Id == PacketId.Punch)
        {// Punch
            var w = new Tinyhand.IO.TinyhandWriter(initialBuffer);
            var span = w.GetSpan(PacketHelper.HeaderSize);
            w.Advance(PacketHelper.HeaderSize);

            var r = new PacketPunchResponse();
            r.EndPoint = endPoint;
            r.UtcTicks = DateTime.UtcNow.Ticks;

            var written = w.Written;
            TinyhandSerializer.Serialize(ref w, r);
            fixed (byte* pb = span)
            {
                header.Id = PacketId.PunchResponse;
                header.DataSize = (ushort)(w.Written - written);
                *(PacketHeader*)pb = header;
            }

            this.unmanagedSends.Enqueue(new UnmanagedSend(endPoint, w.FlushAndGetArray()));
        }
        else
        {// Not supported
        }
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
    private static byte[] initialBuffer = new byte[2048];

    private NetTerminal.GoshujinClass terminals = new();

    private ConcurrentDictionary<ulong, NetTerminalGene> managedGenes = new();

    private ConcurrentQueue<UnmanagedSend> unmanagedSends = new();
}
