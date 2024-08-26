// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Benchmark;

public class FixedByte_ArrayClass
{
    public FixedByte_ArrayClass()
    {
    }

    public byte[] Array = new byte[32];
}

[Config(typeof(BenchmarkConfig))]
public class FixedByteBenchmark
{
    public const int N = 32;

    public FixedByteBenchmark()
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
    public byte[] Test1()
    {
    }
}
