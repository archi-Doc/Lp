// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using BenchmarkDotNet.Attributes;
using Netsphere;
using ValueLink;

namespace Benchmark;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class NetAddressToEndPoint
{
    public NetAddressToEndPoint(NetAddress netAddress)
    {
        this.NetAddress = netAddress;
        var endpoint = default(NetEndpoint);
        this.NetAddress.TryCreateIpv4(ref endpoint);
        this.EndPoint = endpoint.EndPoint;
    }

    [Link(Type = ChainType.Unordered, AddValue = false)]
    public NetAddress NetAddress { get; }

    public EndPoint? EndPoint { get; }
}

[ValueLinkObject]
public partial class NetAddressToEndPoint2
{
    [Link(Type = ChainType.QueueList, Name = "Queue")]
    public NetAddressToEndPoint2(NetAddress netAddress)
    {
        this.NetAddress = netAddress;
        var endpoint = default(NetEndpoint);
        this.NetAddress.TryCreateIpv4(ref endpoint);
        this.EndPoint = endpoint.EndPoint;
    }

    [Link(Type = ChainType.Unordered, AddValue = false)]
    public NetAddress NetAddress { get; }

    public EndPoint? EndPoint { get; }
}

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
    private readonly NetAddress netAddress;
    private readonly NetAddressToEndPoint.GoshujinClass items = new();
    private readonly NetAddressToEndPoint2.GoshujinClass items2 = new();

    public AddressBenchmark()
    {
        this.dualAddress = new(1, 22, 33, 4444, 555555);
        this.dualAddressImplemented = new(1, 22, 33, 4444, 555555);
        this.dualAddressRecord = new(1, 22, 33, 4444, 555555);
        this.dualAddress2 = this.dualAddress;
        this.dualAddressImplemented2 = this.dualAddressImplemented;
        this.dualAddressRecord2 = this.dualAddressRecord;
        this.netAddress = new(22, 33, 4444, 5555);
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
    public EndPoint? NetAddressToEndPoint_Ipv4()
    {
        var endpoint = default(NetEndpoint);
        this.netAddress.TryCreateIpv4(ref endpoint);
        return endpoint.EndPoint;
    }

    [Benchmark]
    public EndPoint? NetAddressToEndPoint_Ipv6()
    {
        var endpoint = default(NetEndpoint);
        this.netAddress.TryCreateIpv6(ref endpoint);
        return endpoint.EndPoint;
    }

    [Benchmark]
    public EndPoint? NetAddressToEndPoint_Cache()
    {
        using (this.items.LockObject.EnterScope())
        {
            if (!this.items.NetAddressChain.TryGetValue(this.netAddress, out var item))
            {
                item = new(this.netAddress);
                this.items.Add(item);
            }

            return item.EndPoint;
        }
    }

    [Benchmark]
    public EndPoint? NetAddressToEndPoint_Cache2()
    {
        // using (this.items.LockObject.EnterScope())
        {
            if (!this.items.NetAddressChain.TryGetValue(this.netAddress, out var item))
            {
                item = new(this.netAddress);
                this.items.Add(item);
            }

            return item.EndPoint;
        }
    }

    [Benchmark]
    public EndPoint? NetAddressToEndPoint_Cache3()
    {
        if (!this.items2.NetAddressChain.TryGetValue(this.netAddress, out var item))
        {
            item = new(this.netAddress);
            this.items2.Add(item);
        }

        return item.EndPoint;
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

    /*[Benchmark]
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
    }*/
}
