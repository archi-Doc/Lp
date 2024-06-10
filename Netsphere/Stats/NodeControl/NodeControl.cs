// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true)]
public sealed partial class NodeControl
{
    private const int ValidTimeInMinutes = 5;
    private const int FailureLimit = 3;
    private static readonly long LifelineValidMics = Mics.FromMinutes(5); // Mics.FromHours(1);
    private static readonly long OnlineValidMics = Mics.FromMinutes(5);

    public NodeControl(NetBase netBase)
    {
        this.netBase = netBase;
    }

    private readonly NetBase netBase;

    #region FieldAndProperty

    private readonly object syncObject = new();

    [Key(0)]
    private LifelineNode.GoshujinClass lifelineNodes = default!;

    [Key(1)]
    private OnlineNode.GoshujinClass onlineNodes = default!;

    #endregion

    public bool TryGetLifelineNode([NotNullWhen(true)] out NetNode? node)
    {
        node = default;
        lock (this.syncObject)
        {
            var obj = this.lifelineNodes.UncheckedListChain.First;
            if (obj is null)
            {
                return false;
            }

            /*if (range.IsWithin(obj.LastCheckedMics))
            {
                // return false;
            }*/

            node = obj;
            this.lifelineNodes.UncheckedListChain.Remove(obj);
        }

        return true;
    }

    public void ReportLifelineNode(NetNode node, ConnectionResult result)
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
                    {
                        g.OnlineLinkChain.AddLast(item);
                        g.OfflineLinkChain.Remove(item);
                    }
                    else
                    {
                        g.OnlineLinkChain.Remove(item);
                        g.OfflineLinkChain.AddLast(item);
                    }
                }
            }

            if (result == ConnectionResult.Success)
            {
                var item2 = this.onlineNodes.AddressChain.FindFirst(node.Address);
                if (item2 is null)
                {
                    item2 = new(node);
                    item2.Goshujin = this.onlineNodes;
                }

                item2.LastConnectionMicsValue = Mics.FastSystem;
            }
            else if (result == ConnectionResult.Failure)
            {
            }
        }
    }

    public void ReportOnlineNode(NetNode node, ConnectionResult result)
    {
        lock (this.syncObject)
        {
            var item = this.onlineNodes.AddressChain.FindFirst(node.Address);
            if (item is null)
            {
                return;
            }

            if (result == ConnectionResult.Success)
            {
                item.LastConnectionMicsValue = Mics.GetSystem();
            }
            else if (result == ConnectionResult.Failure)
            {
                item.Goshujin = default;
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

    internal void Prepare(string nodeList)
    {
        // Load NetOptions.NodeList
        var range = MicsRange.FromPastToFastSystem(LifelineValidMics);
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

            if (range.IsWithin(item.LastCheckedMics))
            {
                continue;
            }

            if (item.Goshujin is { } g)
            {
                g.UncheckedListChain.AddFirst(item);
                g.OfflineLinkChain.Remove(item);
                g.OfflineLinkChain.Remove(item);
            }
        }

        this.ValidateInternal();
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
