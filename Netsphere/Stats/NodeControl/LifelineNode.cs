// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere.Stats;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
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

    public LifelineNode(NetAddress netAddress, EncryptionPublicKey2 publicKey)
    {
        this.Address = netAddress;
        this.PublicKey = publicKey;
    }

    #region FieldAndProperty

    [Key(2)]
    public long LastCheckedMics { get; set; }

    #endregion
}
