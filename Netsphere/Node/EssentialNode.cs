// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using CrystalData;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace Netsphere;

public enum NodeConnectionResult
{
    Success,
    Failure,
}

public partial class EssentialNode
{
    private const string Filename = "EssentialNode.tinyhand";
    private const int ValidTimeInMinutes = 5;

    [TinyhandObject(LockObject = "syncObject", ExplicitKeyOnly = true)]
    private sealed partial class Data
    {
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
        public object syncObject = new();

        [Key(0)]
        public EssentialNodeAddress.GoshujinClass goshujin = new();
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
    }

    public EssentialNode(UnitLogger logger, NetBase netBase, Crystalizer crystalizer)
    {
        this.logger = logger;
        this.netBase = netBase;

        this.crystal = crystalizer.GetOrCreateCrystal<Data>(new CrystalConfiguration() with
        {
            SaveFormat = SaveFormat.Utf8,
            FileConfiguration = new RelativeFileConfiguration(Filename),
            NumberOfHistoryFiles = 0,
        });

        this.data = this.crystal.Data;

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
                if (!this.data.goshujin.AddressChain.ContainsKey(address))
                {
                    this.data.goshujin.Add(new EssentialNodeAddress(address));
                }
            }
        }

        // Unchecked Queue
        var mics = Mics.GetSystem();
        this.data.goshujin.UncheckedChain.Clear();
        foreach (var x in this.data.goshujin.LinkedListChain)
        {
            if (x.ValidMics <= mics && mics <= (x.ValidMics + Mics.FromMinutes(ValidTimeInMinutes)))
            {// [x.ValidMics, x.ValidMics + Mics.FromMinutes(ValidTimeInMinutes)]
            }
            else
            {
                this.data.goshujin.UncheckedChain.Enqueue(x);
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

        lock (this.data.syncObject)
        {
            if (this.data.goshujin.AddressChain.ContainsKey(nodeAddress))
            {// Already exists
                return false;
            }

            var x = new EssentialNodeAddress(nodeAddress);
            this.data.goshujin.Add(x);
            this.data.goshujin.UncheckedChain.Enqueue(x);
        }

        return true;
    }

    public bool GetUncheckedNode([NotNullWhen(true)] out NodeAddress? nodeAddress)
    {
        nodeAddress = null;
        lock (this.data.syncObject)
        {
            if (this.data.goshujin.UncheckedChain.TryDequeue(out var node))
            {
                this.data.goshujin.UncheckedChain.Enqueue(node);
                nodeAddress = node.Address;
                return true;
            }
        }

        return false;
    }

    public bool GetNode([NotNullWhen(true)] out NodeAddress? nodeAddress)
    {
        nodeAddress = null;
        lock (this.data.syncObject)
        {
            var node = this.data.goshujin.LinkedListChain.First;
            if (node != null)
            {
                this.data.goshujin.LinkedListChain.Remove(node);
                this.data.goshujin.LinkedListChain.AddLast(node);
                nodeAddress = node.Address;
                return true;
            }
        }

        return false;
    }

    public void Report(NodeAddress nodeAddress, NodeConnectionResult result)
    {
        lock (this.data.syncObject)
        {
            var node = this.data.goshujin.AddressChain.FindFirst(nodeAddress);
            if (node != null)
            {
                if (node.UncheckedLink.IsLinked)
                {// Unchecked
                    if (result == NodeConnectionResult.Success)
                    {// Success
                        node.UpdateValidMics();
                        this.data.goshujin.UncheckedChain.Remove(node);
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

        lock (this.data.syncObject)
        {
            foreach (var x in this.data.goshujin)
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
        foreach (var x in this.data.goshujin.LinkedListChain)
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
    private ICrystal<Data> crystal;
    private Data data;
}

/// <summary>
/// Represents an essential node information.
/// </summary>
[ValueLinkObject]
[TinyhandObject]
internal partial class EssentialNodeAddress
{
    public const int FailureLimit = 3;

    [Link(Type = ChainType.Unordered)]
    [Key(0)]
    public NodeAddress Address { get; private set; }

    [Link(Type = ChainType.LinkedList, Name = "LinkedList", Primary = true)]
    [Link(Type = ChainType.QueueList, Name = "Unchecked")]
    public EssentialNodeAddress(NodeAddress address)
    {
        this.Address = address;
    }

    public EssentialNodeAddress()
    {
        this.Address = new();
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
