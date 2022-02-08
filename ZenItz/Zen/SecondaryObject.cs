// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
internal partial class SecondaryObject
{
    internal SecondaryObject()
    {// For serializer
    }

    internal SecondaryObject(Identifier secondaryId)
    {// New object
        this.SecondaryId = secondaryId;
        this.FlakeId = FlakeControl.Instance.GetFlakeId();
    }

    internal void Set(byte[] data)
    {
    }

    [Key(0)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    public Identifier SecondaryId { get; private set; }

    [Key(1)]
    public int FlakeId { get; private set; }

    [Key(2)]
    public long Position { get; private set; } = -1; // -1: Not saved

    [Key(3)]
    public int Size { get; private set; }
}
