// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

/*public class FixedByte_ArrayClass
{
    public FixedByte_ArrayClass()
    {
    }

    public byte[] Array { get; set; } = new byte[32];
}

public class FixedByte_StructClass
{
    private readonly struct FixedByte
    {
        private readonly ulong x0;
        private readonly ulong x1;
        private readonly ulong x2;
        private readonly ulong x3;
    }

    public FixedByte_StructClass()
    {
    }

    private FixedByte fixedByte;
}

[Config(typeof(BenchmarkConfig))]
public class FixedByteBenchmark
{
    public const int N = 32;

    private readonly byte[] fixedArray = new byte[N];
    private readonly FixedByte_ArrayClass arrayClass = new();
    private readonly FixedByte_StructClass structClass = new();

    public FixedByteBenchmark()
    {
        RandomVault.Xoshiro.NextBytes(this.fixedArray);
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
    public FixedByte_ArrayClass Test1()
    {
        return this.arrayClass;
    }
}*/
