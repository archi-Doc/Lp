// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using Netsphere;
using Tinyhand;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class AddressBenchmark
{
    public const string Address = "192.168.1.1:1234";

    public AddressBenchmark()
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
    public string ParseAndFormat_NodeAddress()
    {
        NodeAddress.TryParse(Address, out var node);
        return node!.ToString();
    }

    [Benchmark]
    public string ParseAndFormat_DualAddress()
    {
        DualAddress.TryParse(Address, out var node);
        return node!.ToString();
    }

    [Benchmark]
    public NodeAddress? Serialize_NodeAddress()
    {
        var node = new NodeAddress(new(123456), 123);
        var bin = TinyhandSerializer.SerializeObject(node);
        var node2 = TinyhandSerializer.DeserializeObject<NodeAddress>(bin);
        return node2;
    }

    [Benchmark]
    public DualAddress? Serialize_DualAddress()
    {
        var node = new DualAddress(123, 123456, 0, 0, 0);
        var bin = TinyhandSerializer.SerializeObject(node);
        var node2 = TinyhandSerializer.DeserializeObject<DualAddress>(bin);
        return node2;
    }
}
