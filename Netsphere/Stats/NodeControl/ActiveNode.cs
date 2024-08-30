﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
        public const int DefaultMaxItems = 100;

        public static readonly ObjectPool<Integrality> Pool = new(
            () => new()
            {
                MaxItems = DefaultMaxItems,
                RemoveIfItemNotFound = false,
            },
            NetConstants.IntegralityDefaultPoolSize);
    }

    [Link(Primary = true, Type = ChainType.QueueList, Name = "Get")]
    [Link(Unique = true, Type = ChainType.Unordered, TargetMember = "Address", AddValue = false)]
    public ActiveNode()
    {
    }

    public ActiveNode(NetNode netNode)
    {
        this.Address = netNode.Address;
        this.PublicKey = netNode.PublicKey;
    }

    public ActiveNode(NetAddress netAddress, NodePublicKey publicKey)
    {
        this.Address = netAddress;
        this.PublicKey = publicKey;
    }

    #region FieldAndProperty

    [Key(2)]
    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    public long LastConnectionMics { get; private set; }

    #endregion
}
