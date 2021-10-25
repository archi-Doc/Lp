// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace LP.Net;

public class NetTerminal
{
    public NetTerminal Create(NodeAddress nodeAddress)
    {
        var gene = Random.Crypto.NextULong();
        var terminal = new Terminal(gene, nodeAddress);
        this.GeneToTerminal.TryAdd(gene, terminal);
    }

    public NetTerminal()
    {
    }

    public ConcurrentDictionary<ulong, Terminal> GeneToTerminal { get; } = new();
}
