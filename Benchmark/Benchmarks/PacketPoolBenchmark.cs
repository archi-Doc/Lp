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

[Config(typeof(BenchmarkConfig))]
public class PacketPoolBenchmark
{
    public const int N = 100;

    [Params(1024, 2048)]
    public int Length { get; set; }

    public ConcurrentQueue<byte[]> Queue { get; set; } = new();

    public FixedArrayPool FixedArrayPool { get; set; } = default!;

    public ByteArrayPool ByteArrayPool { get; set; } = default!;

    public ArrayPool<byte> ArrayPool { get; set; } = default!;

    public byte[][] Arrays { get; set; } = default!;

    public ByteArrayPool.Owner[] OwnerArray { get; set; } = default!;

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
        this.OwnerArray = new ByteArrayPool.Owner[N];
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
    public byte[] NewArray()
    {
        return new byte[this.Length];
    }

    [Benchmark]
    public byte[] ArrayPool()
    {
        var array = ArrayPool<byte>.Shared.Rent(this.Length);
        ArrayPool<byte>.Shared.Return(array);
        return array;
    }

    [Benchmark]
    public byte[] ConcurrentQueue()
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
    }

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

    [Benchmark]
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
    }

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
}
