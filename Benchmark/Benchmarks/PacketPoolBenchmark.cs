// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using LP;

namespace Benchmark;

public readonly struct ArrayMemoryPair
{
    public ArrayMemoryPair(byte[] array, Memory<byte> memory)
    {
        this.Array = array;
        this.Memory = memory;
    }

    public readonly byte[] Array;
    public readonly Memory<byte> Memory;
}

[Config(typeof(BenchmarkConfig))]
public class PacketPoolBenchmark
{
    public const int N = 100;

    [Params(2048)]
    public int Length { get; set; }

    public ConcurrentQueue<byte[]> Queue { get; set; } = new();

    public FixedArrayPoolObsolete FixedArrayPoolObsolete { get; set; } = default!;

    public FixedArrayPool FixedArrayPool { get; set; } = default!;

    public ArrayPool<byte> ArrayPool { get; set; } = default!;

    public byte[][] Arrays { get; set; } = default!;

    public ArrayMemoryPair[] ArrayMemoryPairs { get; set; } = default!;

    public FixedArrayPool.Owner[] OwnerArray { get; set; } = default!;

    public FixedArrayPool.MemoryOwner[] MemoryArray { get; set; } = default!;

    public IMemoryOwner<byte>[] MemoryOwnerArray { get; set; } = default!;

    public PacketPoolBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.FixedArrayPoolObsolete = new(this.Length, N);
        this.FixedArrayPool = new(this.Length, N);
        this.Arrays = new byte[N][];
        this.ArrayMemoryPairs = new ArrayMemoryPair[N];
        this.OwnerArray = new FixedArrayPool.Owner[N];
        this.MemoryArray = new FixedArrayPool.MemoryOwner[N];
        this.MemoryOwnerArray = new IMemoryOwner<byte>[N];
        for (var n = 0; n < N; n++)
        {
            this.Queue.Enqueue(new byte[this.Length]);
        }

        this.ArrayPool = ArrayPool<byte>.Create(this.Length, N);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public byte[] NewArray1()
    {
        return new byte[this.Length];
    }

    [Benchmark]
    public byte[] ArrayPool1()
    {
        var array = ArrayPool<byte>.Shared.Rent(this.Length);
        ArrayPool<byte>.Shared.Return(array);
        return array;
    }

    [Benchmark]
    public byte[] ConcurrentQueue1()
    {
        byte[]? array;
        if (!this.Queue.TryDequeue(out array))
        {
            array = new byte[this.Length];
        }

        this.Queue.Enqueue(array);
        return array;
    }

    [Benchmark]
    public byte[] FixedArrayPool1()
    {
        var array = this.FixedArrayPoolObsolete.Rent();
        this.FixedArrayPoolObsolete.Return(array);
        return array;
    }

    [Benchmark]
    public FixedArrayPool.Owner Owner1()
    {
        var owner = this.FixedArrayPool.Rent();
        owner.Return();
        return owner;
    }

    [Benchmark]
    public FixedArrayPool.MemoryOwner MemoryOwner1()
    {
        var owner = this.FixedArrayPool.Rent().ToMemoryOwner(0, 10);
        return owner.Return();
    }

    [Benchmark]
    public byte[][] NewArrayN()
    {
        for (var n = 0; n < N; n++)
        {
            this.Arrays[n] = new byte[this.Length];
        }

        return this.Arrays;
    }

    /*[Benchmark]
    public byte[][] ArrayPoolN()
    {
        for (var n = 0; n < N; n++)
        {
            this.Arrays[n] = ArrayPool<byte>.Shared.Rent(this.Length);
        }

        for (var n = 0; n < N; n++)
        {
            ArrayPool<byte>.Shared.Return(this.Arrays[n]);
        }

        return this.Arrays;
    }*/

    [Benchmark]
    public byte[][] ArrayPoolN()
    {
        for (var n = 0; n < N; n++)
        {
            this.Arrays[n] = this.ArrayPool.Rent(this.Length);
        }

        for (var n = 0; n < N; n++)
        {
            this.ArrayPool.Return(this.Arrays[n]);
        }

        return this.Arrays;
    }

    /*[Benchmark]
    public IMemoryOwner<byte>[] MemoryPoolN()
    {
        for (var n = 0; n < N; n++)
        {// Surprisingly, MemoryPool is virtually the same as ArrayPool.
            this.MemoryOwnerArray[n] = MemoryPool<byte>.Shared.Rent(this.Length);
        }

        for (var n = 0; n < N; n++)
        {
            this.MemoryOwnerArray[n].Dispose();
        }

        return this.MemoryOwnerArray;
    }*/

    [Benchmark]
    public byte[][] ConcurrentQueueN()
    {
        for (var n = 0; n < N; n++)
        {
            byte[]? array;
            if (!this.Queue.TryDequeue(out array))
            {
                array = new byte[this.Length];
            }

            this.Arrays[n] = array;
        }

        for (var n = 0; n < N; n++)
        {
            this.Queue.Enqueue(this.Arrays[n]);
        }

        return this.Arrays;
    }

    [Benchmark]
    public byte[][] FixedArrayPoolN()
    {
        for (var n = 0; n < N; n++)
        {
            this.Arrays[n] = this.FixedArrayPoolObsolete.Rent();
        }

        for (var n = 0; n < N; n++)
        {
            this.FixedArrayPoolObsolete.Return(this.Arrays[n]);
        }

        return this.Arrays;
    }

    [Benchmark]
    public ArrayMemoryPair[] ArrayMemoryPairsN()
    {
        for (var n = 0; n < N; n++)
        {
            byte[]? array;
            if (!this.Queue.TryDequeue(out array))
            {
                array = new byte[this.Length];
            }

            this.ArrayMemoryPairs[n] = new(array, new(array, 0, 10));
        }

        for (var n = 0; n < N; n++)
        {
            this.Queue.Enqueue(this.ArrayMemoryPairs[n].Array);
        }

        return this.ArrayMemoryPairs;
    }

    [Benchmark]
    public FixedArrayPool.Owner[] OwnerN()
    {
        for (var n = 0; n < N; n++)
        {
            this.OwnerArray[n] = this.FixedArrayPool.Rent();
        }

        for (var n = 0; n < N; n++)
        {
            this.OwnerArray[n].Return();
        }

        return this.OwnerArray;
    }

    [Benchmark]
    public FixedArrayPool.MemoryOwner[] MemoryOwnerN()
    {
        for (var n = 0; n < N; n++)
        {
            this.MemoryArray[n] = this.FixedArrayPool.Rent().ToMemoryOwner(0, 10);
        }

        for (var n = 0; n < N; n++)
        {
            this.MemoryArray[n].Owner?.Return();
        }

        return this.MemoryArray;
    }
}
