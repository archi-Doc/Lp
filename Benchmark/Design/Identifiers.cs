// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Benchmark.Design;

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

    public Identifier_ClassULong((ulong Id0, ulong Id1, ulong Id2, ulong Id3) id)
    {
        this.Id0 = id.Id0;
        this.Id1 = id.Id1;
        this.Id2 = id.Id2;
        this.Id3 = id.Id3;
    }

    public bool TryWriteBytes(Span<byte> destination)
    {
        var d = destination;
        BitConverter.TryWriteBytes(d, this.Id0);
        d = d.Slice(8);
        BitConverter.TryWriteBytes(d, this.Id1);
        d = d.Slice(8);
        BitConverter.TryWriteBytes(d, this.Id2);
        d = d.Slice(8);
        BitConverter.TryWriteBytes(d, this.Id3);
        return true;
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

    public bool TryWriteBytes(Span<byte> destination)
    {
        this.Identifier.CopyTo(destination);
        return true;
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

    public bool TryWriteBytes(Span<byte> destination)
    {
        this.Identifier.CopyTo(destination);
        return true;
    }

    public byte[] Identifier { get; set; }
}
