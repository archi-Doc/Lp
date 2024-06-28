// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ValueLink.Integrality;

namespace Netsphere.Stats;

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public sealed partial class OnlineNode : NetNode
{
    internal class Integrality : Integrality<OnlineNode.GoshujinClass, OnlineNode>
    {
        public static readonly Integrality Instance = new()
        {
            MaxItems = 100,
            RemoveIfItemNotFound = false,
        };
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Address", AddValue = false)]
    public OnlineNode()
    {
    }

    public OnlineNode(NetNode netNode)
    {
        this.Address = netNode.Address;
        this.PublicKey = netNode.PublicKey;
    }

    #region FieldAndProperty

    [Key(2)]
    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    public long LastConnectionMics { get; private set; }

    #endregion
}
