// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using Arc.Threading;

#pragma warning disable SA1401

namespace LP.Net;

// [ValueLinkObject]
public partial class NetTerminalGene// : IEquatable<NetTerminalGene>
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

    // [Link(Type = ChainType.Ordered)]
    public ulong Gene { get; private set; }

    public NetTerminal NetTerminal { get; }

    public byte[]? Data { get; set; }

    public long InvokeTicks { get; set; }

    public long CompleteTicks { get; set; }

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
