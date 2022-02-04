// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[ValueLinkObject]
internal partial class SecondaryObject
{
    internal SecondaryObject(Identifier secondaryId)
    {
        this.SecondaryId = secondaryId;
    }

    internal void Set(byte[] data)
    {
    }

    [Link(Primary = true, Type = ChainType.Unordered)]
    public Identifier SecondaryId { get; protected set; }
}
