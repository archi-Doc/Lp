// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP.Net;

public enum NodeConnectionResult
{
    Success,
    Failure,
}

public class EssentialNode
{
    public const string FileName = "EssentialNode.tinyhand";
    public const int ValidTimeInMinutes = 5;

    public EssentialNode(Information information)
    {
        this.information = information;

        Radio.Open<Message.Configure>(this.Configure);
        Radio.OpenAsync<Message.LoadAsync>(this.Load);
        Radio.OpenAsync<Message.SaveAsync>(this.Save);
    }

    public void Configure(Message.Configure message)
    {
    }

    public async Task Load(Message.LoadAsync message)
    {
        try
        {
            var path = Path.Combine(this.information.RootDirectory, FileName);
            if (File.Exists(path))
            {
                var bytes = await File.ReadAllBytesAsync(path);
                // this.essentialNodes = TinyhandSerializer.Deserialize<EssentialNodeAddress.GoshujinClass>(bytes);
                var g = TinyhandSerializer.DeserializeFromUtf8<EssentialNodeAddress.GoshujinClass>(bytes);
                if (g != null)
                {
                    this.essentialNodes = g;
                }
            }
        }
        catch
        {
            Log.Error($"Load error: {FileName}");
        }

        // Load NetsphereOptions.Nodes
        var nodes = this.information.ConsoleOptions.NetsphereOptions.Nodes;
        foreach (var x in nodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (NodeAddress.TryParse(x, out var address))
            {
                if (!this.essentialNodes.AddressChain.ContainsKey(address))
                {
                    this.essentialNodes.Add(new EssentialNodeAddress(address));
                }
            }
        }

        // Unchecked Queue
        var ticks = Ticks.GetCurrent();
        this.essentialNodes.UncheckedChain.Clear();
        foreach (var x in this.essentialNodes.LinkedListChain)
        {
            if (x.ValidTicks <= ticks && ticks <= (x.ValidTicks + Ticks.FromMinutes(ValidTimeInMinutes)))
            {// [x.ValidTicks, x.ValidTicks + Ticks.FromMinutes(ValidTimeInMinutes)]
            }
            else
            {
                this.essentialNodes.UncheckedChain.Enqueue(x);
            }
        }

        this.Validate();
    }

    public async Task Save(Message.SaveAsync message)
    {
        var path = Path.Combine(this.information.RootDirectory, FileName);
        using (var file = File.Open(path, FileMode.Create))
        {
            byte[] b;
            lock (this.essentialNodes)
            {
                b = TinyhandSerializer.SerializeToUtf8(this.essentialNodes); // TinyhandSerializer.Serialize(this.essentialNodes)
            }

            file.Write(b);
        }
    }

    public bool GetUncheckedNode([NotNullWhen(true)] out NodeAddress? nodeAddress)
    {
        nodeAddress = null;
        lock (this.essentialNodes)
        {
            if (this.essentialNodes.UncheckedChain.TryDequeue(out var node))
            {
                this.essentialNodes.UncheckedChain.Enqueue(node);
                nodeAddress = node.Address;
                return true;
            }
        }

        return false;
    }

    public bool GetNode([NotNullWhen(true)] out NodeAddress? nodeAddress)
    {
        nodeAddress = null;
        lock (this.essentialNodes)
        {
            var node = this.essentialNodes.LinkedListChain.First;
            if (node != null)
            {
                this.essentialNodes.LinkedListChain.Remove(node);
                this.essentialNodes.LinkedListChain.AddLast(node);
                nodeAddress = node.Address;
                return true;
            }
        }

        return false;
    }

    public void Report(NodeAddress nodeAddress, NodeConnectionResult result)
    {
        lock (this.essentialNodes)
        {
            var node = this.essentialNodes.AddressChain.FindFirst(nodeAddress);
            if (node != null)
            {
                if (node.UncheckedLink.IsLinked)
                {// Unchecked
                    if (result == NodeConnectionResult.Success)
                    {// Success
                        node.UpdateValidTicks();
                        this.essentialNodes.UncheckedChain.Remove(node);
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
                        node.UpdateValidTicks();
                    }
                }
            }
        }
    }

    private void Validate()
    {
        // Validate essential nodes.
        List<EssentialNodeAddress> toDelete = new();
        foreach (var x in this.essentialNodes.LinkedListChain)
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

    private Information information;
    private EssentialNodeAddress.GoshujinClass essentialNodes = new();
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

    public void UpdateValidTicks()
    {
        this.ValidTicks = Ticks.GetCurrent();
        this.FailureCount = 0;
    }

    [Key(1)]
    public long ValidTicks { get; private set; }

    [IgnoreMember]
    public int FailureCount { get; private set; }
}
