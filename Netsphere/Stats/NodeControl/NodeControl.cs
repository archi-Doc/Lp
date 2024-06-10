// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true)]
public sealed partial class NodeControl
{
    private const int ValidTimeInMinutes = 5;
    private const int FailureLimit = 3;
    private static readonly long LifelineValidMics = Mics.FromHours(1);
    private static readonly long OnlineValidMics = Mics.FromMinutes(5);

    public NodeControl(NetBase netBase)
    {
        this.netBase = netBase;
    }

    private readonly NetBase netBase;

    #region FieldAndProperty

    private readonly object syncObject = new();

    [Key(0)]
    private OnlineNode.GoshujinClass lifelineNodes = new();

    [Key(1)]
    private OnlineNode.GoshujinClass onlineNodes = new();

    #endregion

    public bool TryGetLifelineNode([NotNullWhen(true)] out NetNode? node)
    {
        var range = MicsRange.FromFastSystemInDuration(LifelineValidMics);
        node = default;
        lock (this.syncObject)
        {
            var obj = this.lifelineNodes.LastConnectionMicsChain.First;
            if (obj is null)
            {
                return false;
            }

            if (range.IsWithin(obj.LastConnectionMics))
            {//
            }

            node = obj;
            obj.Goshujin = default;
        }

        return true;
    }

    public void ReportLifelineNode(NetNode node, ConnectionResult result)
    {
        lock (this.syncObject)
        {
            var item = this.lifelineNodes.AddressChain.FindFirst(node.Address);
            if (item is null)
            {
                return;
            }

            if (result == ConnectionResult.Success)
            {
                var item2 = this.onlineNodes.AddressChain.FindFirst(node.Address);
                if (item2 is null)
                {
                    item2 = new(node);
                    item2.Goshujin = this.onlineNodes;
                }

                item2.LastConnectionMicsValue = Mics.GetSystem();
            }
            else if (result == ConnectionResult.Failure)
            {
                item.Goshujin = default;
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
        var nodes = this.netBase.NetOptions.NodeList;
        foreach (var x in nodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!NetNode.TryParse(x, out var node))
            {
                continue;
            }

            if (this.lifelineNodes.AddressChain.TryGetValue(node.Address, out var obj))
            {
                continue;
            }

            this.lifelineNodes.Add(new(node));
        }

        this.ValidateInternal();
    }

    private void ValidateInternal()
    {
        // Validate nodes.
        List<OnlineNode> toDelete = new();
        foreach (var x in this.lifelineNodes)
        {
            if (!x.Validate())
            {
                toDelete.Add(x);
            }
        }

        foreach (var x in this.onlineNodes)
        {
            if (!x.Validate())
            {
                toDelete.Add(x);
            }
        }

        foreach (var x in toDelete)
        {
            x.Goshujin = null;
        }
    }
}
