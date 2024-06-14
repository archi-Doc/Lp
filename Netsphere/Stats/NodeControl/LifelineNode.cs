// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Stats;

[TinyhandObject]
[ValueLinkObject]
public sealed partial class LifelineNode : NetNode
{
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Address", AddValue = false)]
    [Link(Type = ChainType.LinkedList, Name = "UncheckedList", AutoLink = false)]
    [Link(Type = ChainType.LinkedList, Name = "OnlineLink", AutoLink = false)]
    [Link(Type = ChainType.LinkedList, Name = "OfflineLink", AutoLink = false)]
    public LifelineNode()
    {
    }

    public LifelineNode(NetNode netNode)
    {
        this.Address = netNode.Address;
        this.PublicKey = netNode.PublicKey;
    }

    #region FieldAndProperty

    [Key(2)]
    public long LastCheckedMics { get; set; }

    #endregion
}
