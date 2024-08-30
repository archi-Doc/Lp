// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using ValueLink.Integrality;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true)]
public sealed partial class NodeControl : ITinyhandSerializationCallback
{
    public static readonly int MaxLifelineNodes = 32;
    public static readonly int SufficientLifelineNodes = 24;
    public static readonly int MaxActiveNodes = 256;
    public static readonly int MaxUnknownNodes = 32;
    public static readonly int SufficientActiveNodes = 128;
    private static readonly long LifelineCheckIntervalMics = Mics.FromMinutes(5); // Mics.FromHours(1);
    private static readonly long OnlineValidMics = Mics.FromMinutes(5);

    public NodeControl(NetBase netBase)
    {
        this.netBase = netBase;
    }

    private readonly NetBase netBase;

    #region FieldAndProperty

    [Key(0)]
    private LifelineNode.GoshujinClass lifelineNodes = new(); // this.lifelineNodes.SyncObject

    [Key(1)]
    private ActiveNode.GoshujinClass activeNodes = new(); // this.activeNodes.SyncObject

    private ActiveNode.GoshujinClass unknownNodes = new(); // this.unknownNodes.SyncObject

    public int CountLinfelineOnline => this.lifelineNodes.OnlineLinkChain.Count;

    public int CountLinfelineOffline => this.lifelineNodes.OfflineLinkChain.Count;

    public int CountActive => this.activeNodes.Count;

    public int CountUnknown => this.unknownNodes.Count;

    public bool CanAddLifelineNode => this.lifelineNodes.Count < MaxLifelineNodes;

    public bool CanAddActiveNode => this.activeNodes.Count < MaxActiveNodes;

    public bool HasSufficientActiveNodes => this.CountActive >= SufficientActiveNodes;

    #endregion

    public void FromLifelineNodeToActiveNode()
    {
        lock (this.lifelineNodes.SyncObject)
        {
            lock (this.activeNodes.SyncObject)
            {
                foreach (var x in this.lifelineNodes.OnlineLinkChain)
                {
                    if (this.activeNodes.AddressChain.ContainsKey(x.Address))
                    {
                        continue;
                    }

                    if (!this.CanAddActiveNode)
                    {
                        return;
                    }

                    var item = new ActiveNode(x.Address, x.PublicKey);
                    this.activeNodes.Add(item);
                }
            }
        }
    }

    /// <summary>
    /// Maintains the lifeline nodes by adding online nodes to the lifeline and removing offline lifeline nodes if there are sufficient lifeline nodes.
    /// </summary>
    public void MaintainLifelineNode()
    {
        if (!this.CanAddLifelineNode)
        {
            return;
        }

        lock (this.lifelineNodes.SyncObject)
        {
            lock (this.activeNodes.SyncObject)
            {
                // Online -> Lifeline
                foreach (var x in this.activeNodes)
                {
                    if (this.lifelineNodes.AddressChain.ContainsKey(x.Address))
                    {
                        continue;
                    }

                    if (!this.CanAddLifelineNode)
                    {
                        return;
                    }

                    var item = new LifelineNode(x.Address, x.PublicKey);
                    item.LastCheckedMics = Mics.FastSystem;
                    this.lifelineNodes.Add(item);
                    this.lifelineNodes.OnlineLinkChain.AddLast(item);
                }

                // Lifeline offline -> Remove
                var deleteList = new DeferredList<LifelineNode.GoshujinClass, LifelineNode>(this.lifelineNodes);
                foreach (var x in this.lifelineNodes.OfflineLinkChain)
                {
                    if ((this.lifelineNodes.Count - deleteList.Count) > SufficientLifelineNodes)
                    {
                        deleteList.Add(x);
                    }
                }

                deleteList.DeferredRemove();
            }
        }
    }

    /// <summary>
    /// Tries to add a NetNode from the incoming connection and check the node later.
    /// </summary>
    /// <param name="node">The NetNode to add.</param>
    /// <returns>True if the node was added successfully, false otherwise.</returns>
    public bool TryAddUnknownNode(NetNode node)
    {
        if (this.HasSufficientActiveNodes)
        {
            return false;
        }

        if (this.unknownNodes.Count >= MaxUnknownNodes)
        {
            return false;
        }

        lock (this.activeNodes.SyncObject)
        {
            if (this.activeNodes.AddressChain.ContainsKey(node.Address))
            {
                return false;
            }
        }

        lock (this.unknownNodes.SyncObject)
        {
            var item = new ActiveNode(node);
            item.Goshujin = this.unknownNodes;
        }

        return true;
    }

    public bool TryGetUncheckedLifelineNode([MaybeNullWhen(false)] out NetNode node)
    {
        node = default;
        lock (this.lifelineNodes.SyncObject)
        {
            var obj = this.lifelineNodes.UncheckedListChain.First;
            if (obj is null)
            {
                return false;
            }

            node = obj;
            this.lifelineNodes.UncheckedListChain.Remove(obj);
        }

        return true;
    }

    public bool TryGetActiveNode([MaybeNullWhen(false)] out NetNode node)
    {
        node = default;
        lock (this.activeNodes.SyncObject)
        {
            if (!this.activeNodes.GetChain.TryDequeue(out var obj))
            {
                return false;
            }

            this.activeNodes.GetChain.Enqueue(obj);
            node = obj;
        }

        return true;
    }

    public bool TryGetUnknownNode([MaybeNullWhen(false)] out NetNode node)
    {
        node = default;
        lock (this.unknownNodes.SyncObject)
        {
            if (!this.unknownNodes.GetChain.TryPeek(out var obj))
            {
                return false;
            }

            obj.Goshujin = default;
            node = obj;
        }

        return true;
    }

    public BytePool.RentMemory DifferentiateActiveNode(ReadOnlyMemory<byte> memory)
    {
        var integrality = ActiveNode.Integrality.Pool.Get();// Necessary?
        var result = ((IIntegralityObject)this.activeNodes).Differentiate(integrality, memory);
        ActiveNode.Integrality.Pool.Return(integrality);
        return result;
    }

    public Task<IntegralityResult> IntegrateOnlineNode(IntegralityBrokerDelegate brokerDelegate, CancellationToken cancellationToken)
    {
        var integrality = ActiveNode.Integrality.Pool.Get();
        var result = integrality.Integrate(this.activeNodes, brokerDelegate, cancellationToken);
        ActiveNode.Integrality.Pool.Return(integrality);
        return result;
    }

    public void ReportLifelineNodeConnection(NetNode node, ConnectionResult result)
    {
        lock (this.lifelineNodes.SyncObject)
        {
            lock (this.activeNodes.SyncObject)
            {
                var item = this.lifelineNodes.AddressChain.FindFirst(node.Address);
                if (item is not null)
                {
                    item.LastCheckedMics = Mics.FastSystem;
                    if (item.Goshujin is { } g)
                    {
                        g.UncheckedListChain.Remove(item);
                        if (result == ConnectionResult.Success)
                        {// Online link
                            g.OnlineLinkChain.AddLast(item);
                            g.OfflineLinkChain.Remove(item);
                        }
                        else
                        {// Offline link
                            g.OnlineLinkChain.Remove(item);
                            g.OfflineLinkChain.AddLast(item);
                        }
                    }
                }

                if (result == ConnectionResult.Success)
                {
                    var item2 = this.activeNodes.AddressChain.FindFirst(node.Address);
                    if (item2 is not null)
                    {
                        item2.LastConnectionMicsValue = Mics.FastSystem;
                    }
                    else if (this.CanAddActiveNode)
                    {
                        item2 = new(node);
                        item2.LastConnectionMicsValue = Mics.FastSystem;
                        item2.Goshujin = this.activeNodes;
                    }
                }
                else if (result == ConnectionResult.Failure)
                {
                }
            }
        }
    }

    public void ReportActiveNodeConnection(NetNode node, ConnectionResult result)
    {
        lock (this.activeNodes.SyncObject)
        {
            var item = this.activeNodes.AddressChain.FindFirst(node.Address);
            if (item is not null)
            {
                if (result == ConnectionResult.Success)
                {
                    item.LastConnectionMicsValue = Mics.FastSystem;
                }
                else if (result == ConnectionResult.Failure)
                {
                    item.Goshujin = default;
                }
            }
            else
            {
                if (result == ConnectionResult.Success &&
                    this.CanAddActiveNode)
                {
                    item = new(node);
                    item.LastConnectionMicsValue = Mics.FastSystem;
                    item.Goshujin = this.activeNodes;
                }
            }
        }
    }

    public string Dump()
    {
        string st;
        st = $"Lifeline nodes: {this.lifelineNodes.Count}, Online nodes: {this.activeNodes.Count}";
        return st;
    }

    public void Validate()
    {
        lock (this.lifelineNodes.SyncObject)
        {
            lock (this.activeNodes.SyncObject)
            {
                this.ValidateInternal();
            }
        }
    }

    public void Trim()
    {
        lock (this.lifelineNodes.SyncObject)
        {
            lock (this.activeNodes.SyncObject)
            {
                this.TrimInternal();
            }
        }
    }

    void ITinyhandSerializationCallback.OnBeforeSerialize()
    {
    }

    void ITinyhandSerializationCallback.OnAfterDeserialize()
    {
        this.LoadNodeList();
        this.Prepare();
    }

    void ITinyhandSerializationCallback.OnAfterReconstruct()
    {
        this.LoadNodeList();
    }

    internal void Prepare()
    {
        List<LifelineNode>? offlineToUnchecked = default;
        foreach (var x in this.lifelineNodes.OfflineLinkChain)
        {// Offline -> Unchecked
            offlineToUnchecked ??= new();
            offlineToUnchecked.Add(x);
        }

        if (offlineToUnchecked is not null)
        {
            foreach (var x in offlineToUnchecked)
            {
                this.lifelineNodes.OfflineLinkChain.Remove(x);
                this.lifelineNodes.UncheckedListChain.AddFirst(x);
            }
        }

        this.ValidateInternal();
        this.TrimInternal();
    }

    private void LoadNodeList()
    {// Load NetOptions.NodeList
        var nodes = this.netBase.NetOptions.NodeList;
        foreach (var x in nodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!NetNode.TryParse(x, out var node))
            {
                continue;
            }

            if (!this.lifelineNodes.AddressChain.TryGetValue(node.Address, out var item))
            {// New
                item = new LifelineNode(node);
                this.lifelineNodes.Add(item);
                this.lifelineNodes.UncheckedListChain.AddFirst(item);
                continue;
            }
        }
    }

    private void TrimInternal()
    {
        var lifelineRange = MicsRange.FromPastToFastSystem(LifelineCheckIntervalMics);
        foreach (var x in this.lifelineNodes)
        {
            if (!lifelineRange.IsWithin(x.LastCheckedMics) &&
                x.Goshujin is { } g)
            {// Online/Offline -> Unchecked
                g.UncheckedListChain.AddFirst(x);
                g.OfflineLinkChain.Remove(x);
                g.OfflineLinkChain.Remove(x);
            }
        }

        var onlineRange = MicsRange.FromPastToFastSystem(OnlineValidMics);
        List<ActiveNode>? deleteList = default;
        foreach (var x in this.activeNodes)
        {
            if (!lifelineRange.IsWithin(x.LastConnectionMics) &&
                x.Goshujin is { } g)
            {
                deleteList ??= new();
                deleteList.Add(x);
            }
        }

        if (deleteList is not null)
        {
            foreach (var x in deleteList)
            {
                x.Goshujin = default;
            }
        }
    }

    private void ValidateInternal()
    {
        // Validate nodes.
        List<LifelineNode>? lifelineList = default;
        foreach (var x in this.lifelineNodes)
        {
            if (!x.Validate())
            {
                lifelineList ??= new();
                lifelineList.Add(x);
            }
        }

        if (lifelineList is not null)
        {
            foreach (var x in lifelineList)
            {
                x.Goshujin = null;
            }
        }

        List<ActiveNode>? onlineList = default;
        foreach (var x in this.activeNodes)
        {
            if (!x.Validate())
            {
                onlineList ??= new();
                onlineList.Add(x);
            }
        }

        if (onlineList is not null)
        {
            foreach (var x in onlineList)
            {
                x.Goshujin = null;
            }
        }
    }
}
