// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Crypto;
using Benchmark.Design;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark;

[TinyhandObject]
public readonly partial struct ChecksumStruct
{
    public ChecksumStruct(ulong a, ulong b, ulong c, ulong d)
    {
        this.A = a;
        this.B = b;
        this.C = c;
        this.D = d;
    }

    [Key(0)]
    public readonly ulong A;

    [Key(1)]
    public readonly ulong B;

    [Key(2)]
    public readonly ulong C;

    [Key(3)]
    public readonly ulong D;

    public ulong GetChecksum()
    {
        var writer = default(TinyhandWriter);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this);
            return FarmHash.Hash64(writer.FlushAndGetReadOnlySpan());
        }
        finally
        {
            writer.Dispose();
        }
    }

    public unsafe ulong GetChecksum2()
    {
        return FarmHash.Hash64(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref Unsafe.AsRef(this)), sizeof(ChecksumStruct)));
    }

    public unsafe ulong GetChecksum3()
    {
        Span<byte> buffer = stackalloc byte[32];
        var span = buffer;
        BitConverter.TryWriteBytes(span, this.A);
        span = span.Slice(8);
        BitConverter.TryWriteBytes(span, this.B);
        span = span.Slice(8);
        BitConverter.TryWriteBytes(span, this.C);
        span = span.Slice(8);
        BitConverter.TryWriteBytes(span, this.D);

        return FarmHash.Hash64(buffer);
    }

}

[Config(typeof(BenchmarkConfig))]
public class ChecksumBenchmark
{
    public ChecksumStruct TestStruct { get; set; }

    public ChecksumBenchmark()
    {
        this.TestStruct = new(1, 2222, 3333333, 4444444444);

        var d1 = default(ChecksumStruct);
        var c1 = d1.GetChecksum();
        var c2 = d1.GetChecksum2();
        var c3 = d1.GetChecksum3();

        d1 = new ChecksumStruct(1, 0, 0, 0);
        c1 = d1.GetChecksum();
        c2 = d1.GetChecksum2();
        c3 = d1.GetChecksum3();

        d1 = this.TestStruct;
        c1 = d1.GetChecksum();
        c2 = d1.GetChecksum2();
        c3 = d1.GetChecksum3();
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public ulong Checksum()
        => this.TestStruct.GetChecksum();

    [Benchmark]
    public ulong Checksum2()
        => this.TestStruct.GetChecksum2();

    [Benchmark]
    public ulong Checksum3()
        => this.TestStruct.GetChecksum3();
}
