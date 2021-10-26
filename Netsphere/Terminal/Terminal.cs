// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace LP.Net;

public class Terminal
{
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

    private NetTerminal.GoshujinClass terminals = new();

    private ConcurrentDictionary<ulong, NetTerminalGene> recvGenes = new();
}
