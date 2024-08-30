// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class ServiceIdBenchmark
{
    private readonly struct ServiceObject
    {
        public readonly int Index;
        public readonly object? Instance;

        public ServiceObject(int index, object? instance)
        {
            this.Index = index;
            this.Instance = instance;
        }
    }

    private const uint C1 = 0x4b51b15f;
    private const uint C2 = 0xc1457fc2;
    private readonly uint[] array = [0x4b51b15f, 0x5951f1b8, 0x2f36e1e4, 0xd3a6b566, 0x4b471076, 0x2d3534d8, 0x5bef11d1, 0xc1457fc2, ];
    private readonly UInt32Hashtable<ServiceObject> hashtable = new();
    private readonly object[] objects = new object[8];

    public ServiceIdBenchmark()
    {
        for (var i = 0; i < this.array.Length; i++)
        {
            this.hashtable.TryAdd(this.array[i], new(i, default));
        }
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
    public bool Hashtable()
    {
        var objects = new object[8];

        if (this.hashtable.TryGetValue(C1, out var obj1))
        {
            objects[obj1.Index] = new object();
        }

        if (this.hashtable.TryGetValue(C2, out var obj2))
        {
            objects[obj2.Index] = new object();
        }

        bool b = false;
        if (this.hashtable.TryGetValue(C1, out obj1))
        {
            if (this.hashtable.TryGetValue(C2, out obj2))
            {
                b = objects[obj1.Index] == objects[obj2.Index];
            }
        }

        return b;
    }

    [Benchmark]
    public bool Hashtable2()
    {
        UInt32Hashtable<ServiceObject> table = new();

        table.GetOrAdd(C1, x => new(0, new object()));
        table.GetOrAdd(C2, x => new(0, new object()));

        bool b = false;
        if (table.TryGetValue(C1, out var obj1))
        {
            if (table.TryGetValue(C2, out var obj2))
            {
                b = obj1.Instance == obj2.Instance;
            }
        }

        return b;
    }
}
