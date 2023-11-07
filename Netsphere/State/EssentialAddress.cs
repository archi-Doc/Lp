// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using BigMachines;
using CrystalData;

namespace Netsphere;

public enum ConnectionResult
{
    Success,
    Failure,
}

public sealed partial class EssentialAddress
{
    private const int ValidTimeInMinutes = 5;
    private const int FailureLimit = 3;

    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    [TinyhandObject]
    internal partial class Item
    {
        [Link(Type = ChainType.Unordered)]
        [Key(0)]
        public DualAddress Address { get; private set; }

        [Link(Primary = true, Type = ChainType.LinkedList, Name = "LinkedList")]
        [Link(Type = ChainType.QueueList, Name = "Unchecked")]
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

        [Key(1)]
        public long ValidMics { get; private set; }

        [IgnoreMember]
        public int FailureCount { get; private set; }

        public override string ToString()
            => $"{this.Address.ToString()}, Valid: {Mics.ToString(this.ValidMics)}, Failed: {this.FailureCount}";
    }

    public EssentialAddress(UnitLogger logger, NetBase netBase, Crystalizer crystalizer)
    {
        this.logger = logger;
        this.netBase = netBase;

        this.Prepare();
    }

    public void Prepare()
    {
        // Load NetsphereOptions.Nodes
        var nodes = this.netBase.NetsphereOptions.Nodes;
        foreach (var x in nodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (NodeAddress.TryParse(x, out var address))
            {
                if (!this.data.AddressChain.ContainsKey(address))
                {
                    this.data.Add(new EssentialNodeAddress(address));
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

    public bool TryAdd(NodeAddress nodeAddress)
    {
        if (!nodeAddress.IsValid())
        {
            return false;
        }

        lock (this.data.SyncObject)
        {
            if (this.data.AddressChain.ContainsKey(nodeAddress))
            {// Already exists
                return false;
            }

            var x = new EssentialNodeAddress(nodeAddress);
            this.data.Add(x);
            this.data.UncheckedChain.Enqueue(x);
        }

        return true;
    }

    public bool GetUncheckedNode([NotNullWhen(true)] out NodeAddress? nodeAddress)
    {
        nodeAddress = null;
        lock (this.data.SyncObject)
        {
            if (this.data.UncheckedChain.TryDequeue(out var node))
            {
                this.data.UncheckedChain.Enqueue(node);
                nodeAddress = node.Address;
                return true;
            }
        }

        return false;
    }

    public bool GetNode([NotNullWhen(true)] out NodeAddress? nodeAddress)
    {
        nodeAddress = null;
        lock (this.data.SyncObject)
        {
            var node = this.data.LinkedListChain.First;
            if (node != null)
            {
                this.data.LinkedListChain.Remove(node);
                this.data.LinkedListChain.AddLast(node);
                nodeAddress = node.Address;
                return true;
            }
        }

        return false;
    }

    public void Report(NodeAddress nodeAddress, NodeConnectionResult result)
    {
        lock (this.data.SyncObject)
        {
            var node = this.data.AddressChain.FindFirst(nodeAddress);
            if (node != null)
            {
                if (node.UncheckedLink.IsLinked)
                {// Unchecked
                    if (result == NodeConnectionResult.Success)
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
                    if (result == NodeConnectionResult.Success)
                    {// Success
                        node.UpdateValidMics();
                    }
                }
            }
        }
    }

    public List<string> Dump()
    {
        List<string> list = new();

        lock (this.data.SyncObject)
        {
            foreach (var x in this.data)
            {
                list.Add(x.ToString()!);
            }
        }

        return list;
    }

    private void Validate()
    {
        // Validate essential nodes.
        List<EssentialNodeAddress> toDelete = new();
        foreach (var x in this.data.LinkedListChain)
        {
            if (!x.Address.IsValid())
            {
                toDelete.Add(x);
            }
        }

        foreach (var x in toDelete)
        {
            x.Goshujin = null;
        }
    }

    private NetBase netBase;
    private UnitLogger logger;
    private Item.GoshujinClass data;
}
