// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class RandomUsageBenchmark
{
    public Random Random { get; set; } = new(42);

    public ObjectPool<Random> Pool { get; set; } = new(() => new Random());

    public RandomUsageBenchmark()
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
    public int Raw()
    {
        return this.Random.Next();
    }

    [Benchmark]
    public int Lock()
    {
        lock (this.Random)
        {
            return this.Random.Next();
        }
    }

    [Benchmark]
    public int ObjectPool()
    {
        var r = this.Pool.Get();
        try
        {
            return r.Next();
        }
        finally
        {
            this.Pool.Return(r);
        }
    }

    [Benchmark]
    public int New()
    {
        var r = new Random(12);
        return r.Next();
    }
}
