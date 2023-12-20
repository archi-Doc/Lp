// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class ResendListBenchmark
{
    public record Item(int Id);

    public const int N = 1_000;

    private readonly object syncObject = new();
    private Item[] items;
    private Queue<Item> queue = new();
    private List<Item> list = new();
    private OrderedMap<int, Item> orderedMap = new();

    public ResendListBenchmark()
    {
        this.items = new Item[N];
        for (var i = 0; i < N; i++)
        {
            this.items[i] = new(i);
        }

        foreach (var x in this.items)
        {
            this.queue.Enqueue(x);
            this.list.Add(x);
            this.orderedMap.Add(x.Id, x);
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
    public Item TestQueue()
    {
        lock (this.syncObject)
        {
            var item = this.queue.Dequeue();
            this.queue.Enqueue(item);
            return item;
        }
    }

    [Benchmark]
    public int TestList()
    {
        lock (this.syncObject)
        {
            var i = 0;
            foreach (var x in this.list)
            {
                if (x.Id == N / 2)
                {
                    i++;
                }
            }

            return i;
        }
    }

    [Benchmark]
    public OrderedMap<int, Item>.Node? TestOrderedMap()
    {
        lock (this.syncObject)
        {
            var node = this.orderedMap.FindNode(N / 2);
            if (node is not null)
            {
                var item = node.Value;
                this.orderedMap.RemoveNode(node);
                this.orderedMap.Add(N / 2, item, node);
            }

            return node;
        }
    }
}
