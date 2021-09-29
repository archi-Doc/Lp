// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP.Net;

public class Node
{
    public Node()
    {
        Radio.OpenAsync<Message.DeserializeAsync>(this.Deserialize);
    }

    public async Task Deserialize(Message.DeserializeAsync message)
    {
        Console.WriteLine("Node.Deserialize");
    }

    private EssentialNodeAddress.GoshujinClass essentialNodes = new();
}

/// <summary>
/// Represents an essential node information.
/// </summary>
[ValueLinkObject]
[TinyhandObject]
public partial class EssentialNodeAddress : NodeAddress
{
    [Link(Type = ChainType.QueueList, Name = "Queue", Primary = true)]
    public EssentialNodeAddress()
    {
    }
}
