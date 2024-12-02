// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

public readonly struct StructEmbryo
{
    private readonly ulong x0;
    private readonly ulong x1;
    private readonly ulong x2;
    private readonly ulong x3;
    private readonly ulong x4;
    private readonly ulong x5;
    private readonly ulong x6;
    private readonly ulong x7;

    [UnscopedRef]
    public Span<byte> Span
        => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this), 1));
}

public class StructEmbryoClass
{
    public StructEmbryoClass()
    {
    }

    public void CreateEmbryo(ReadOnlySpan<byte> data)
    {
        Blake2B.Get512_Span(data, this.embryo.Span);
    }

    public ReadOnlySpan<byte> Key => this.embryo.Span.Slice(32, 32);

    private readonly StructEmbryo embryo;
}

public class ByteArrayEmbryoClass
{
    public ByteArrayEmbryoClass()
    {
    }

    public void CreateEmbryo(ReadOnlySpan<byte> data)
    {
        Blake2B.Get512_Span(data, this.embryo);
    }

    public ReadOnlySpan<byte> Key => this.embryo.AsSpan(32, 32);

    private readonly byte[] embryo = new byte[64];
}

[Config(typeof(BenchmarkConfig))]
public class EmbryoBenchmark
{
    public const int Length = 128;

    private readonly byte[] data;
    private readonly StructEmbryoClass structClass;
    private readonly ByteArrayEmbryoClass byteArrayClass;
    private readonly byte[] key;

    public EmbryoBenchmark()
    {
        var r = new Arc.Crypto.Xoshiro256StarStar(12);
        this.data = new byte[Length];
        this.key = new byte[CryptoBox.SecretKeySize];
        r.NextBytes(this.data);

        this.structClass = this.CreateEmbryo_Struct();
        this.byteArrayClass = this.CreateEmbryo_ByteArray();
    }

    [Benchmark]
    public StructEmbryoClass CreateEmbryo_Struct()
    {
        var c = new StructEmbryoClass();
        c.CreateEmbryo(this.data);
        return c;
    }

    [Benchmark]
    public ByteArrayEmbryoClass CreateEmbryo_ByteArray()
    {
        var c = new ByteArrayEmbryoClass();
        c.CreateEmbryo(this.data);
        return c;
    }

    [Benchmark]
    public byte CopyKey_Struct()
    {
        this.structClass.Key.CopyTo(this.key);
        return this.key[0];
    }

    [Benchmark]
    public byte CopyKey_ByteArray()
    {
        this.byteArrayClass.Key.CopyTo(this.key);
        return this.key[0];
    }
}
