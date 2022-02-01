// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[ValueLinkObject]
public partial class PrimaryObject
{
    internal PrimaryObject(Identifier primaryId, uint shipId)
    {
        this.PrimaryId = primaryId;
    }

    [Link(Primary = true, Type = ChainType.Ordered)]
    public Identifier PrimaryId { get; protected set; }
}
