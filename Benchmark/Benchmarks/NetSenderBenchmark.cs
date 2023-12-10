// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Net;
using Arc.Unit;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class NetSenderBenchmark
{
    public readonly struct Item
    {
        public Item(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
        {
            this.EndPoint = endPoint;
            this.MemoryOwner = toBeShared;
        }

        public readonly IPEndPoint EndPoint;

        public readonly ByteArrayPool.MemoryOwner MemoryOwner;
    }

    private IPEndPoint endPoint;
    private ByteArrayPool.MemoryOwner memoryOwner;
    private Queue<Item> items = new();
    private ConcurrentQueue<Item> items2 = new();

    public NetSenderBenchmark()
    {
        this.endPoint = new(IPAddress.Loopback, 1234);
        this.memoryOwner = new ByteArrayPool.MemoryOwner(new byte[100]);
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
    public Item TestQueue()
    {
        this.items.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items.Dequeue();
        this.items.Dequeue();
        this.items.Dequeue();
        this.items.Dequeue();
        return this.items.Dequeue();
    }

    [Benchmark]
    public Item TestConcurrentQueue()
    {
        this.items2.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items2.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items2.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items2.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items2.Enqueue(new(this.endPoint, this.memoryOwner));
        this.items2.TryDequeue(out var item);
        this.items2.TryDequeue(out item);
        this.items2.TryDequeue(out item);
        this.items2.TryDequeue(out item);
        this.items2.TryDequeue(out item);
        return item;
    }
}
