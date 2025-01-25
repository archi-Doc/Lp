// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.VisualBasic;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Netsphere.Stats;

[TinyhandObject]
[ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public sealed partial class ActiveNode : NetNode
{
    internal class Integrality : Integrality<ActiveNode.GoshujinClass, ActiveNode>
    {
        public static readonly Integrality Default = new()
        {
            MaxItems = 100,
            RemoveIfItemNotFound = false,
        };
    }

    public ActiveNode(NetNode netNode)
    {
        this.Address = netNode.Address;
        this.PublicKey = netNode.PublicKey;
    }

    public ActiveNode(NetAddress netAddress, EncryptionPublicKey publicKey)
    {
        this.Address = netAddress;
        this.PublicKey = publicKey;
    }

    [Link(Primary = true, Type = ChainType.QueueList, Name = "Get")]
    [Link(Unique = true, Type = ChainType.Unordered, TargetMember = "Address", AddValue = false)]
    private ActiveNode()
    {
    }

    #region FieldAndProperty

    [Key(2)]
    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    public long LastConnectionMics { get; private set; }

    #endregion
}
