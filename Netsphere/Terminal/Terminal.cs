// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;

namespace LP.Net;

public class Terminal
{
    /// <summary>
    /// Create raw (without public key) NetTerminal instance.
    /// </summary>
    /// <param name="nodeAddress">NodeAddress.</param>
    /// <returns>NetTerminal.</returns>
    public NetTerminal Create(NodeAddress nodeAddress)
    {
        var gene = Random.Crypto.NextULong();
        var terminal = new NetTerminal(gene, nodeAddress);
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

        if (this.recvGenes.TryGetValue(header.Gene, out var terminalGene))
        {
            terminalGene.NetTerminal.ProcessRecv(terminalGene, endPoint, ref header, data);
        }
        else
        {
            this.ProcessUnregisteredRecv(endPoint, ref header, data);
        }
    }

    internal void ProcessUnregisteredRecv(IPEndPoint endPoint, ref PacketHeader header, byte[]data)
    {
    }

    private NetTerminal.GoshujinClass terminals = new();

    private ConcurrentDictionary<ulong, NetTerminalGene> recvGenes = new();
}
