// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class GenePoolBenchmark
{
    [Params(100)]
    public int Length { get; set; }

    public Xoshiro256StarStar Xoshiro { get; } = new();

    public MersenneTwister Mt { get; } = new();

    public GenePoolBenchmark()
    {
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
    public ulong[] Xoshiro_NextULong()
    {
        var array = new ulong[this.Length];
        for (var i = 0; i < this.Length; i++)
        {
            array[i] = this.Xoshiro.NextULong();
        }

        return array;
    }

    [Benchmark]
    public ulong[] Mt_NextULong()
    {
        var array = new ulong[this.Length];
        for (var i = 0; i < this.Length; i++)
        {
            array[i] = this.Mt.NextULong();
        }

        return array;
    }
}
