// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true)]
public sealed partial class EssentialNode : ITinyhandSerializationCallback
{
    private const int ValidTimeInMinutes = 5;
    private const int FailureLimit = 3;

    public EssentialNode(NetBase netBase)
    {
        this.netBase = netBase;
    }

    private readonly NetBase netBase;

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
        public NetNode Node { get; private set; }

        [Key(1)]
        public long ValidMics { get; private set; }

        [IgnoreMember]
        public int FailureCount { get; private set; }

        [Link(Primary = true, Type = ChainType.LinkedList, Name = "LinkedList")]
        [Link(Type = ChainType.QueueList, Name = "Unchecked")]
        [Link(Type = ChainType.LinkedList, Name = "Ipv4List", AutoLink = false)]
        [Link(Type = ChainType.LinkedList, Name = "Ipv6List", AutoLink = false)]
        public Item(NetNode node)
        {
            this.Node = node;
        }

        public Item()
        {
            this.Node = default!; // Do not use default values as instances are reused during deserialization, leading to inconsistency.
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
            => $"{this.Node.ToString()}, Valid: {Mics.ToString(this.ValidMics)}, Failed: {this.FailureCount}";
    }

    public bool TryAdd(NetNode node)
    {
        if (!node.Validate())
        {
            return false;
        }

        lock (this.data.SyncObject)
        {
            if (this.data.NodeChain.ContainsKey(node))
            {// Already exists
                return false;
            }

            var x = new Item(node);
            this.data.Add(x);
            this.data.UncheckedChain.Enqueue(x);
        }

        return true;
    }

    public bool GetUncheckedNode([NotNullWhen(true)] out NetNode? node)
    {
        node = default;
        lock (this.data.SyncObject)
        {
            if (this.data.UncheckedChain.TryDequeue(out var item))
            {
                this.data.UncheckedChain.Enqueue(item);
                node = item.Node;
                return true;
            }
        }

        return false;
    }

    public bool GetNode([NotNullWhen(true)] out NetNode? node)
    {
        node = null;
        lock (this.data.SyncObject)
        {
            var item = this.data.LinkedListChain.First;
            if (item is not null)
            {
                this.data.LinkedListChain.Remove(item);
                this.data.LinkedListChain.AddLast(item);
                node = item.Node;
                return true;
            }
        }

        return false;
    }

    public void Report(NetNode node, ConnectionResult result)
    {
        lock (this.data.SyncObject)
        {
            var item = this.data.NodeChain.FindFirst(node);
            if (item != null)
            {
                if (item.UncheckedLink.IsLinked)
                {// Unchecked
                    if (result == ConnectionResult.Success)
                    {// Success
                        item.UpdateValidMics();
                        this.data.UncheckedChain.Remove(item);
                    }
                    else
                    {// Failure
                        if (item.IncrementFailureCount())
                        {// Remove
                            item.Goshujin = null;
                        }
                    }
                }
                else
                {// Checked
                    if (result == ConnectionResult.Success)
                    {// Success
                        item.UpdateValidMics();
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
            st = $"Ipv4/Ipv6 {this.data.Ipv4ListChain.Count}/{this.data.Ipv6ListChain.Count}";
        }

        return st;
    }

    public void Validate()
    {
        // Validate essential nodes.
        List<Item> toDelete = new();
        foreach (var x in this.data.LinkedListChain)
        {
            if (!x.Node.Validate())
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
        var nodes = this.netBase.NetOptions.NodeList;
        foreach (var x in nodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!NetNode.TryParse(x, out var node) ||
                this.data.NodeChain.ContainsKey(node))
            {
                continue;
            }

            var item = new Item(node);
            this.data.Add(item);
            if (node.Address.IsValidIpv4)
            {
                this.data.Ipv4ListChain.AddLast(item);
            }

            if (node.Address.IsValidIpv6)
            {
                this.data.Ipv6ListChain.AddLast(item);
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
