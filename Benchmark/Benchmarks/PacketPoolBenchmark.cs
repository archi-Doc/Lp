// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using Arc.Collections;
using BenchmarkDotNet.Attributes;
using Lp;

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

    [Params(1024)]
    public int Length { get; set; }

    public ConcurrentQueue<byte[]> Queue { get; set; } = new();

    // public FixedArrayPoolObsolete FixedArrayPoolObsolete { get; set; } = default!;

    public FixedArrayPool FixedArrayPool { get; set; } = default!;

    public BytePool ByteArrayPool { get; set; } = default!;

    // public ByteArrayPoolObsolete ByteArrayPoolObsolete { get; set; } = default!;

    public ArrayPool<byte> ArrayPool { get; set; } = default!;

    public byte[][] Arrays { get; set; } = default!;

    public ArrayMemoryPair[] ArrayMemoryPairs { get; set; } = default!;

    public FixedArrayPool.Owner[] OwnerArray { get; set; } = default!;

    public BytePool.RentArray[] OwnerArray2 { get; set; } = default!;

    // public ByteArrayPoolObsolete.Owner[] OwnerArray3 { get; set; } = default!;

    public FixedArrayPool.MemoryOwner[] MemoryArray { get; set; } = default!;

    public BytePool.RentMemory[] MemoryArray2 { get; set; } = default!;

    // public ByteArrayPoolObsolete.MemoryOwner[] MemoryArray3 { get; set; } = default!;

    // public IMemoryOwner<byte>[] MemoryOwnerArray { get; set; } = default!;

    public PacketPoolBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        // this.FixedArrayPoolObsolete = new(this.Length, N);
        this.FixedArrayPool = new(this.Length, N);
        this.ByteArrayPool = BytePool.CreateExponential();
        // this.ByteArrayPoolObsolete = new(this.Length, N);
        this.Arrays = new byte[N][];
        this.ArrayMemoryPairs = new ArrayMemoryPair[N];
        this.OwnerArray = new FixedArrayPool.Owner[N];
        this.OwnerArray2 = new BytePool.RentArray[N];
        // this.OwnerArray3 = new ByteArrayPoolObsolete.Owner[N];
        this.MemoryArray = new FixedArrayPool.MemoryOwner[N];
        this.MemoryArray2 = new BytePool.RentMemory[N];
        // this.MemoryArray3 = new ByteArrayPoolObsolete.MemoryOwner[N];
        // this.MemoryOwnerArray = new IMemoryOwner<byte>[N];
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

    /*[Benchmark]
    public byte[] FixedArrayPoolObsolete1()
    {
        var array = this.FixedArrayPoolObsolete.Rent();
        this.FixedArrayPoolObsolete.Return(array);
        return array;
    }*/

    [Benchmark]
    public FixedArrayPool.Owner FixedOwner1()
    {
        var owner = this.FixedArrayPool.Rent();
        owner.Return();
        return owner;
    }

    [Benchmark]
    public BytePool.RentArray ByteOwner1()
    {
        var owner = this.ByteArrayPool.Rent(this.Length);
        owner.Return();
        return owner;
    }

    /*[Benchmark]
    public ByteArrayPoolObsolete.Owner ByteOwner1()
    {
        var owner = this.ByteArrayPoolObsolete.Rent(this.Length);
        owner.Return();
        return owner;
    }*/

    [Benchmark]
    public FixedArrayPool.MemoryOwner FixedMemoryOwner1()
    {
        var owner = this.FixedArrayPool.Rent().AsMemory(0, 10);
        return owner.Return();
    }

    [Benchmark]
    public BytePool.RentMemory ByteMemoryOwner1()
    {
        var owner = this.ByteArrayPool.Rent(this.Length).AsMemory(0, 10);
        return owner.Return();
    }

    /*[Benchmark]
    public ByteArrayPoolObsolete.MemoryOwner ByteMemoryOwner1()
    {
        var owner = this.ByteArrayPoolObsolete.Rent(this.Length).AsMemory(0, 10);
        return owner.Return();
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

    /*[Benchmark]
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
    }*/

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
    public FixedArrayPool.Owner[] FixedOwnerN()
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
    public BytePool.RentArray[] ByteOwnerN()
    {
        for (var n = 0; n < N; n++)
        {
            this.OwnerArray2[n] = this.ByteArrayPool.Rent(this.Length);
        }

        for (var n = 0; n < N; n++)
        {
            this.OwnerArray2[n].Return();
        }

        return this.OwnerArray2;
    }

    /*[Benchmark]
    public ByteArrayPoolObsolete.Owner[] ByteOwnerN()
    {
        for (var n = 0; n < N; n++)
        {
            this.OwnerArray3[n] = this.ByteArrayPoolObsolete.Rent(this.Length);
        }

        for (var n = 0; n < N; n++)
        {
            this.OwnerArray3[n].Return();
        }

        return this.OwnerArray3;
    }*/

    [Benchmark]
    public FixedArrayPool.MemoryOwner[] FixedMemoryOwnerN()
    {
        for (var n = 0; n < N; n++)
        {
            this.MemoryArray[n] = this.FixedArrayPool.Rent().AsMemory(0, 10);
        }

        for (var n = 0; n < N; n++)
        {
            this.MemoryArray[n].Owner?.Return();
        }

        return this.MemoryArray;
    }

    [Benchmark]
    public BytePool.RentMemory[] ByteMemoryOwnerN()
    {
        for (var n = 0; n < N; n++)
        {
            this.MemoryArray2[n] = this.ByteArrayPool.Rent(this.Length).AsMemory(0, 10);
        }

        for (var n = 0; n < N; n++)
        {
            this.MemoryArray2[n].RentArray?.Return();
        }

        return this.MemoryArray2;
    }

    /*[Benchmark]
    public ByteArrayPoolObsolete.MemoryOwner[] ByteMemoryOwnerN()
    {
        for (var n = 0; n < N; n++)
        {
            this.MemoryArray3[n] = this.ByteArrayPoolObsolete.Rent(this.Length).AsMemory(0, 10);
        }

        for (var n = 0; n < N; n++)
        {
            this.MemoryArray3[n].Owner.Return();
        }

        return this.MemoryArray3;
    }*/
}
