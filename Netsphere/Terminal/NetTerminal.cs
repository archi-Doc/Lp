// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace LP.Net;

public class NetTerminal
{
    public Terminal Create(NodeAddress nodeAddress)
    {
        var gene = Random.Crypto.NextULong();
        var terminal = new Terminal(gene, nodeAddress);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        return terminal;
    }

    public NetTerminal()
    {
    }

    private Terminal.GoshujinClass terminals = new();

    // public ConcurrentDictionary<ulong, Terminal> GeneToTerminal { get; } = new();
}
