// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP.Net;

public class Node
{
    public const string FileName = "node.tinyhand";

    public Node(Information information)
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
        // nodes = "192.168.0.1:100,, [192.168.0.2]:200, 192.168.0.1:100";
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

        this.Validate();
    }

    public async Task Save(Message.SaveAsync message)
    {
        var path = Path.Combine(this.information.RootDirectory, FileName);
        using (var file = File.Open(path, FileMode.Create))
        {
            file.Write(TinyhandSerializer.SerializeToUtf8(this.essentialNodes));
            // file.Write(TinyhandSerializer.Serialize(this.essentialNodes));
        }
    }

    private void Validate()
    {
        // Validate essential nodes.
        List<EssentialNodeAddress> toDelete = new();
        foreach (var x in this.essentialNodes.QueueChain)
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
    [Link(Type = ChainType.Unordered)]
    [Key(0)]
    public NodeAddress Address { get; private set; }

    [Link(Type = ChainType.QueueList, Name = "Queue", Primary = true)]
    public EssentialNodeAddress(NodeAddress address)
    {
        this.Address = address;
    }

    public EssentialNodeAddress()
    {
        this.Address = new();
    }
}
