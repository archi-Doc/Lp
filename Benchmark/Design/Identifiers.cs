// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Benchmark;

public class Identifier_ClassULong
{
    public Identifier_ClassULong()
    {
    }

    public Identifier_ClassULong(ulong id0, ulong id1, ulong id2, ulong id3)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
    }

    public ulong Id0 { get; set; }

    public ulong Id1 { get; set; }

    public ulong Id2 { get; set; }

    public ulong Id3 { get; set; }
}

public class Identifier_ClassByte
{
    public Identifier_ClassByte()
    {
        this.Identifier = new byte[32];
    }

    public Identifier_ClassByte(byte[] identifier)
    {
        this.Identifier = identifier;
    }

    public byte[] Identifier { get; set; }
}

public struct Identifier_StructByte
{
    public Identifier_StructByte()
    {
        this.Identifier = new byte[32];
    }

    public Identifier_StructByte(byte[] identifier)
    {
        this.Identifier = identifier;
    }

    public byte[] Identifier { get; set; }
}
