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

    public FixedArrayPool FixedArrayPool { get; set; } = default!;

    public ByteArrayPool ByteArrayPool { get; set; } = default!;

    public ArrayPool<byte> ArrayPool { get; set; } = default!;

    public byte[][] Arrays { get; set; } = default!;

    public ArrayMemoryPair[] ArrayMemoryPairs { get; set; } = default!;

    public ByteArrayPool.Owner[] OwnerArray { get; set; } = default!;

    public ByteArrayPool.MemoryOwner[] MemoryArray { get; set; } = default!;

    public ByteArrayPool.MemoryOwner2[] MemoryArray2 { get; set; } = default!;

    public IMemoryOwner<byte>[] MemoryOwnerArray { get; set; } = default!;

    public PacketPoolBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.FixedArrayPool = new(this.Length, N);
        this.ByteArrayPool = new(this.Length, N);
        this.Arrays = new byte[N][];
        this.ArrayMemoryPairs = new ArrayMemoryPair[N];
        this.OwnerArray = new ByteArrayPool.Owner[N];
        this.MemoryArray = new ByteArrayPool.MemoryOwner[N];
        this.MemoryArray2 = new ByteArrayPool.MemoryOwner2[N];
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

    /*[Benchmark]
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
    }*/

    [Benchmark]
    public byte[] FixedArrayPool1()
    {
        var array = this.FixedArrayPool.Rent();
        this.FixedArrayPool.Return(array);
        return array;
    }

    [Benchmark]
    public ByteArrayPool.Owner Owner1()
    {
        var owner = this.ByteArrayPool.Rent();
        owner.Return();
        return owner;
    }

    [Benchmark]
    public ByteArrayPool.MemoryOwner MemoryOwner1()
    {
        var owner = this.ByteArrayPool.Rent(0, 10);
        return owner.Return();
    }

    /*[Benchmark]
    public byte[][] NewArrayN()
    {
        for (var n = 0; n < N; n++)
        {
            this.Arrays[n] = new byte[this.Length];
        }

        return this.Arrays;
    }

    [Benchmark]
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
    public byte[][] ArrayPool2N()
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
            this.Arrays[n] = this.FixedArrayPool.Rent();
        }

        for (var n = 0; n < N; n++)
        {
            this.FixedArrayPool.Return(this.Arrays[n]);
        }

        return this.Arrays;
    }

    [Benchmark]
    public ByteArrayPool.Owner[] ByteArrayPoolN()
    {
        for (var n = 0; n < N; n++)
        {
            this.OwnerArray[n] = this.ByteArrayPool.Rent();
        }

        for (var n = 0; n < N; n++)
        {
            this.OwnerArray[n].Return();
        }

        return this.OwnerArray;
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
    public ByteArrayPool.MemoryOwner[] MemoryOwnerN()
    {
        for (var n = 0; n < N; n++)
        {
            this.MemoryArray[n] = this.ByteArrayPool.Rent(0, 10);
        }

        for (var n = 0; n < N; n++)
        {
            this.MemoryArray[n].Owner?.Return();
        }

        return this.MemoryArray;
    }

    [Benchmark]
    public ByteArrayPool.MemoryOwner2[] MemoryOwner2N()
    {
        for (var n = 0; n < N; n++)
        {
            this.MemoryArray2[n] = this.ByteArrayPool.Rent2(0, 10);
        }

        for (var n = 0; n < N; n++)
        {
            this.MemoryArray2[n].Owner?.Return();
        }

        return this.MemoryArray2;
    }
}
