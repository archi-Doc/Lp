// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
public partial struct NetTerminalGene : IEquatable<NetTerminalGene>
{
    public static NetTerminalGene New()
    {
        return new NetTerminalGene(0);
    }

    public NetTerminalGene(ulong gene)
    {
        this.Gene = gene;
    }

    public ulong Gene { get; }

    public bool Equals(NetTerminalGene other) => this.Gene == other.Gene;
}
