﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace LP;

[TinyhandObject]
public partial class Identifier : IEquatable<Identifier>
{
    public static Identifier Zero { get; } = new();

    public static Identifier One { get; } = new(1);

    public static Identifier Two { get; } = new(2);

    public Identifier()
    {
    }

    public Identifier(ulong id0)
    {
        this.Id0 = id0;
    }

    public Identifier(ulong id0, ulong id1, ulong id2, ulong id3)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
    }

    public Identifier(Identifier identifier)
    {
        this.Id0 = identifier.Id0;
        this.Id1 = identifier.Id1;
        this.Id2 = identifier.Id2;
        this.Id3 = identifier.Id3;
    }

    [Key(0)]
    public ulong Id0 { get; set; }

    [Key(1)]
    public ulong Id1 { get; set; }

    [Key(2)]
    public ulong Id2 { get; set; }

    [Key(3)]
    public ulong Id3 { get; set; }

    public bool Equals(Identifier? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Id0 == other.Id0 && this.Id1 == other.Id1 && this.Id2 == other.Id2 && this.Id3 == other.Id3;
    }

    public override int GetHashCode() => HashCode.Combine(this.Id0, this.Id1, this.Id2, this.Id3);

    public override string ToString() => $"Identifier {this.Id0:D16}";
}
