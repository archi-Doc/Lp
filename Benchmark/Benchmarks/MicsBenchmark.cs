// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class MicsBenchmark
{
    public int TimestampToMicsInt { get; }

    public long TimestampToMicsLong { get; }

    public double TimestampToMicsDouble { get; }

    public long Timestamp { get; }

    public double TimestampDouble { get; }

    public MicsBenchmark()
    {
        this.TimestampToMicsInt = (int)(Stopwatch.Frequency / 1_000_000L);
        this.TimestampToMicsLong = Stopwatch.Frequency / 1_000_000L;
        this.TimestampToMicsDouble = 1_000_000d / (double)Stopwatch.Frequency;
        this.Timestamp = Stopwatch.GetTimestamp();
        this.TimestampDouble = Stopwatch.GetTimestamp();
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
    public long ConvertLongLong()
    {
        return this.Timestamp * 1000;
    }

    [Benchmark]
    public long ConvertLongDouble()
    {
        return (long)(this.Timestamp * 1000d);
    }

    [Benchmark]
    public long ConvertDoubleLong()
    {
        return (long)(this.TimestampDouble * 1000);
    }

    [Benchmark]
    public long ConvertDoubleDouble()
    {
        return (long)(this.TimestampDouble * 1000d);
    }

    [Benchmark]
    public long GetTimestamp()
    {
        return Stopwatch.GetTimestamp();
    }

    [Benchmark]
    public long GetTimestamp_MicsInt()
    {
        return Stopwatch.GetTimestamp() / this.TimestampToMicsInt;
    }

    [Benchmark]
    public long GetTimestamp_MicsLong()
    {
        return Stopwatch.GetTimestamp() / this.TimestampToMicsLong;
    }

    [Benchmark]
    public long GetTimestamp_MicsDouble()
    {
        return (long)(Stopwatch.GetTimestamp() * this.TimestampToMicsDouble);
    }

    [Benchmark]
    public long Constant()
    {
        return this.Timestamp;
    }

    [Benchmark]
    public long Constant_MicsInt()
    {
        return this.Timestamp / this.TimestampToMicsInt;
    }

    [Benchmark]
    public long Constant_MicsLong()
    {
        return this.Timestamp / this.TimestampToMicsLong;
    }

    [Benchmark]
    public long Constant_MicsDouble()
    {
        return (long)(this.Timestamp * this.TimestampToMicsDouble);
    }
}
