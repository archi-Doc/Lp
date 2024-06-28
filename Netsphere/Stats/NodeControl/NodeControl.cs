// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using ValueLink.Integrality;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true, LockObject = "syncObject")]
public sealed partial class NodeControl : ITinyhandSerializationCallback
{
    public static readonly int MaxLifelineNodes = 32;
    public static readonly int SufficientLifelineNodes = 24;
    public static readonly int MaxOnlineNodes = 256;
    public static readonly int MaxUnknownNodes = 32;
    public static readonly int SufficientOnlineNodes = 128;
    private static readonly long LifelineCheckIntervalMics = Mics.FromMinutes(5); // Mics.FromHours(1);
    private static readonly long OnlineValidMics = Mics.FromMinutes(5);

    public NodeControl(NetBase netBase)
    {
        this.netBase = netBase;
    }

    private readonly NetBase netBase;

    #region FieldAndProperty

    private readonly object syncObject = new();

    [Key(0)]
    private LifelineNode.GoshujinClass lifelineNodes = new(); // this.syncObject

    [Key(1)]
    private OnlineNode.GoshujinClass onlineNodes = new(); // this.syncObject

    private OnlineNode.GoshujinClass unknownNodes = new(); // this.syncObject

    public int CountLinfelineOnline => this.lifelineNodes.OnlineLinkChain.Count;

    public int CountLinfelineOffline => this.lifelineNodes.OfflineLinkChain.Count;

    public int CountOnline => this.onlineNodes.Count;

    public bool CanAddLifelineNode => this.lifelineNodes.Count < MaxLifelineNodes;

    public bool CanAddOnlineNode => this.onlineNodes.Count < MaxOnlineNodes;

    public bool HasSufficientOnlineNodes => this.CountOnline >= SufficientOnlineNodes;

    #endregion

    public void MaintainLifelineNode()
    {
        if (!this.CanAddLifelineNode)
        {
            return;
        }

        lock (this.syncObject)
        {
            // Online -> Lifeline
            foreach (var x in this.onlineNodes)
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

    /// <summary>
    /// Tries to add a NetNode from the incoming connection and check the node later.
    /// </summary>
    /// <param name="node">The NetNode to add.</param>
    /// <returns>True if the node was added successfully, false otherwise.</returns>
    public bool TryAddUnknownNode(NetNode node)
    {
        if (this.HasSufficientOnlineNodes)
        {
            return false;
        }

        if (this.unknownNodes.Count >= MaxUnknownNodes)
        {
            return false;
        }

        lock (this.syncObject)
        {
            if (this.onlineNodes.AddressChain.ContainsKey(node.Address))
            {
                return false;
            }

            var item = new OnlineNode(node);
            item.Goshujin = this.unknownNodes;
        }

        return true;
    }

    public bool TryGetLifelineNode([MaybeNullWhen(false)] out NetNode node)
    {
        node = default;
        lock (this.syncObject)
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

    public bool TryGetOnlineNode([MaybeNullWhen(false)] out NetNode node)
    {
        node = default;
        lock (this.syncObject)
        {
            var obj = this.onlineNodes.LastConnectionMicsChain.First;
            if (obj is null)
            {
                return false;
            }

            node = obj;
        }

        return true;
    }

    public IntegralityResultMemory DifferentiateOnlineNode(BytePool.RentMemory memory)
    {
        lock (this.syncObject)
        {
            return ((IIntegralityObject)this.onlineNodes).Differentiate(OnlineNode.Integrality.Instance, memory);
        }
    }

    public Task<IntegralityResult> IntegrateOnlineNode(IntegralityBrokerDelegate brokerDelegate, CancellationToken cancellationToken)
    {
        return OnlineNode.Integrality.Instance.Integrate(this.onlineNodes, brokerDelegate, cancellationToken);
    }

    public void ReportLifelineNodeConnection(NetNode node, ConnectionResult result)
    {
        lock (this.syncObject)
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
                var item2 = this.onlineNodes.AddressChain.FindFirst(node.Address);
                if (item2 is not null)
                {
                    item2.LastConnectionMicsValue = Mics.FastSystem;
                }
                else if (this.CanAddOnlineNode)
                {
                    item2 = new(node);
                    item2.LastConnectionMicsValue = Mics.FastSystem;
                    item2.Goshujin = this.onlineNodes;
                }
            }
            else if (result == ConnectionResult.Failure)
            {
            }
        }
    }

    public void ReportOnlineNodeConnection(NetNode node, ConnectionResult result)
    {
        lock (this.syncObject)
        {
            var item = this.onlineNodes.AddressChain.FindFirst(node.Address);
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
                    this.CanAddOnlineNode)
                {
                    item = new(node);
                    item.LastConnectionMicsValue = Mics.FastSystem;
                    item.Goshujin = this.onlineNodes;
                }
            }
        }
    }

    public string Dump()
    {
        string st;
        lock (this.syncObject)
        {
            st = $"Lifeline nodes: {this.lifelineNodes.Count}, Online nodes: {this.onlineNodes.Count}";
        }

        return st;
    }

    public void Validate()
    {
        lock (this.syncObject)
        {
            this.ValidateInternal();
        }
    }

    public void Trim()
    {
        lock (this.syncObject)
        {
            this.TrimInternal();
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
        List<OnlineNode>? deleteList = default;
        foreach (var x in this.onlineNodes)
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

        List<OnlineNode>? onlineList = default;
        foreach (var x in this.onlineNodes)
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
