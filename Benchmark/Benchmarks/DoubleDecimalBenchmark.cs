// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Benchmark;

#pragma warning disable SA1401 // Fields should be private

[Config(typeof(BenchmarkConfig))]
public class DoubleDecimalBenchmark
{
    public double X1 = 2d;
    public double X2 = 3d;
    public double X3 = 4d;

    public decimal Y1 = 2;
    public decimal Y2 = 3;
    public decimal Y3 = 4;

    public DoubleDecimalBenchmark()
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
    public double Double()
    {
        return (this.X1 * Math.Abs(this.X2)) + this.X3;
    }

    [Benchmark]
    public decimal Decimal()
    {
        return (this.Y1 * Math.Abs(this.Y2)) + this.Y3;
    }

    [Benchmark]
    public byte[] SerializeDouble()
    {
        return Tinyhand.TinyhandSerializer.Serialize((this.X1 * this.X2) + this.X3);
    }

    [Benchmark]
    public byte[] SerializeDecimal()
    {
        return Tinyhand.TinyhandSerializer.Serialize((this.Y1 * this.Y2) + this.Y3);
    }
}
