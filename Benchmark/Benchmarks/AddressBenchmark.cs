// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using Netsphere;
using Tinyhand;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class AddressBenchmark
{
    public const string Address = "192.168.1.1:1234";

    private readonly DualAddress dualAddress;
    private readonly DualAddress dualAddress2;
    private readonly DualAddressImplemented dualAddressImplemented;
    private readonly DualAddressImplemented dualAddressImplemented2;
    private readonly DualAddressRecord dualAddressRecord;
    private readonly DualAddressRecord dualAddressRecord2;

    public AddressBenchmark()
    {
        this.dualAddress = new(1, 22, 33, 4444, 555555);
        this.dualAddressImplemented = new(1, 22, 33, 4444, 555555);
        this.dualAddressRecord = new(1, 22, 33, 4444, 555555);
        this.dualAddress2 = this.dualAddress;
        this.dualAddressImplemented2 = this.dualAddressImplemented;
        this.dualAddressRecord2 = this.dualAddressRecord;
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    /*[Benchmark]
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
    }*/

    [Benchmark]
    public DualAddress? Serialize_DualAddress()
    {
        var node = new DualAddress(123, 123456, 0, 0, 0);
        var bin = TinyhandSerializer.SerializeObject(node);
        var node2 = TinyhandSerializer.DeserializeObject<DualAddress>(bin);
        return node2;
    }

    [Benchmark]
    public DualAddressImplemented? Serialize_DualAddressImplemented()
    {
        var node = new DualAddressImplemented(123, 123456, 0, 0, 0);
        var bin = TinyhandSerializer.SerializeObject(node);
        var node2 = TinyhandSerializer.DeserializeObject<DualAddressImplemented>(bin);
        return node2;
    }

    [Benchmark]
    public DualAddressRecord? Serialize_DualAddressRecord()
    {
        var node = new DualAddressRecord(123, 123456, 0, 0, 0);
        var bin = TinyhandSerializer.SerializeObject(node);
        var node2 = TinyhandSerializer.DeserializeObject<DualAddressRecord>(bin);
        return node2;
    }

    [Benchmark]
    public bool Equals_DualAddress()
    {
        var result = this.dualAddress.Equals(this.dualAddress2);
        return result;
    }

    [Benchmark]
    public bool Equals_DualAddressImplemented()
    {
        var result = this.dualAddressImplemented.Equals(this.dualAddressImplemented2);
        return result;
    }

    [Benchmark]
    public bool Equals_DualAddressRecord()
    {
        var result = this.dualAddressRecord.Equals(this.dualAddressRecord2);
        return result;
    }

    [Benchmark]
    public int GetHashCode_DualAddress()
    {
        var result = this.dualAddress.GetHashCode();
        return result;
    }

    [Benchmark]
    public int GetHashCode_DualAddressImplemented()
    {
        var result = this.dualAddressImplemented.GetHashCode();
        return result;
    }

    [Benchmark]
    public int GetHashCode_DualAddressRecord()
    {
        var result = this.dualAddressRecord.GetHashCode();
        return result;
    }
}
