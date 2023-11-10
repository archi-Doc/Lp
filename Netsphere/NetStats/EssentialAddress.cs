// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Netsphere.NetStats;

[TinyhandObject(UseServiceProvider = true)]
public sealed partial class EssentialAddress : ITinyhandSerializationCallback
{
    private const int ValidTimeInMinutes = 5;
    private const int FailureLimit = 3;

    public EssentialAddress(UnitLogger logger, NetBase netBase)
    {
        this.logger = logger;
        this.netBase = netBase;
    }

    private NetBase netBase;
    private UnitLogger logger;

    [Key(0)]
    private Item.GoshujinClass data = new();

    public int CountIpv4 => this.data.Ipv4ListChain.Count;

    public int CountIpv6 => this.data.Ipv6ListChain.Count;

    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    [TinyhandObject]
    internal partial class Item
    {
        [Link(Type = ChainType.Unordered)]
        [Key(0)]
        public DualAddress Address { get; private set; }

        [Key(1)]
        public long ValidMics { get; private set; }

        [IgnoreMember]
        public int FailureCount { get; private set; }

        [Link(Primary = true, Type = ChainType.LinkedList, Name = "LinkedList")]
        [Link(Type = ChainType.QueueList, Name = "Unchecked")]
        [Link(Type = ChainType.LinkedList, Name = "Ipv4List", AutoLink = false)]
        [Link(Type = ChainType.LinkedList, Name = "Ipv6List", AutoLink = false)]
        public Item(DualAddress address)
        {
            this.Address = address;
        }

        public Item()
        {
        }

        public bool IncrementFailureCount()
        {
            return ++this.FailureCount >= FailureLimit;
        }

        public void UpdateValidMics()
        {
            this.ValidMics = Mics.GetSystem();
            this.FailureCount = 0;
        }

        public override string ToString()
            => $"{this.Address.ToString()}, Valid: {Mics.ToString(this.ValidMics)}, Failed: {this.FailureCount}";
    }

    public bool TryAdd(DualAddress address)
    {
        if (!address.Validate())
        {
            return false;
        }

        lock (this.data.SyncObject)
        {
            if (this.data.AddressChain.ContainsKey(address))
            {// Already exists
                return false;
            }

            var x = new Item(address);
            this.data.Add(x);
            this.data.UncheckedChain.Enqueue(x);
        }

        return true;
    }

    public bool GetUncheckedNode([NotNullWhen(true)] out DualAddress? address)
    {
        address = null;
        lock (this.data.SyncObject)
        {
            if (this.data.UncheckedChain.TryDequeue(out var node))
            {
                this.data.UncheckedChain.Enqueue(node);
                address = node.Address;
                return true;
            }
        }

        return false;
    }

    public bool GetNode([NotNullWhen(true)] out DualAddress? address)
    {
        address = null;
        lock (this.data.SyncObject)
        {
            var node = this.data.LinkedListChain.First;
            if (node != null)
            {
                this.data.LinkedListChain.Remove(node);
                this.data.LinkedListChain.AddLast(node);
                address = node.Address;
                return true;
            }
        }

        return false;
    }

    public void Report(DualAddress nodeAddress, ConnectionResult result)
    {
        lock (this.data.SyncObject)
        {
            var node = this.data.AddressChain.FindFirst(nodeAddress);
            if (node != null)
            {
                if (node.UncheckedLink.IsLinked)
                {// Unchecked
                    if (result == ConnectionResult.Success)
                    {// Success
                        node.UpdateValidMics();
                        this.data.UncheckedChain.Remove(node);
                    }
                    else
                    {// Failure
                        if (node.IncrementFailureCount())
                        {// Remove
                            node.Goshujin = null;
                        }
                    }
                }
                else
                {// Checked
                    if (result == ConnectionResult.Success)
                    {// Success
                        node.UpdateValidMics();
                    }
                }
            }
        }
    }

    public string Dump()
    {
        string st;
        lock (this.data.SyncObject)
        {
            st = $"Ipv4+Ipv6/Total {this.data.Ipv4ListChain.Count}+{this.data.Ipv6ListChain.Count}/{this.data.LinkedListChain.Count}";
        }

        return st;
    }

    public void Validate()
    {
        // Validate essential nodes.
        List<Item> toDelete = new();
        foreach (var x in this.data.LinkedListChain)
        {
            if (!x.Address.Validate())
            {
                toDelete.Add(x);
            }
        }

        foreach (var x in toDelete)
        {
            x.Goshujin = null;
        }
    }

    void ITinyhandSerializationCallback.OnBeforeSerialize()
    {
    }

    void ITinyhandSerializationCallback.OnAfterDeserialize()
    {
        this.Prepare();
    }

    private void Prepare()
    {
        // Load NetsphereOptions.Nodes
        var nodes = this.netBase.NetsphereOptions.Nodes;
        foreach (var x in nodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (DualAddress.TryParse(x, out var address))
            {
                if (!this.data.AddressChain.ContainsKey(address))
                {
                    var item = new Item(address);
                    this.data.Add(item);
                    if (address.IsValidIpv4)
                    {
                        this.data.Ipv4ListChain.AddLast(item);
                    }

                    if (address.IsValidIpv6)
                    {
                        this.data.Ipv6ListChain.AddLast(item);
                    }
                }
            }
        }

        // Unchecked Queue
        var mics = Mics.GetSystem();
        this.data.UncheckedChain.Clear();
        foreach (var x in this.data.LinkedListChain)
        {
            if (x.ValidMics <= mics && mics <= (x.ValidMics + Mics.FromMinutes(ValidTimeInMinutes)))
            {// [x.ValidMics, x.ValidMics + Mics.FromMinutes(ValidTimeInMinutes)]
            }
            else
            {
                this.data.UncheckedChain.Enqueue(x);
            }
        }

        this.Validate();
    }
}
