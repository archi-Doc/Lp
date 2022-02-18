// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz.Obsolete;

[TinyhandObject]
public partial class FlakeItem
{
    public FlakeItem()
    {
    }

    [Key(0)]
    public Identifier SecondaryId { get; set; }

    [Key(1)]
    public int FlakeId { get; set; }

    [Key(2)]
    public long Position { get; set; }

    [Key(3)]
    public int Size { get; set; }
}
