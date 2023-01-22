// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz.Obsolete;

[TinyhandObject]
public partial class FlakeHeader
{
    public FlakeHeader()
    {
    }

    [Key(0)]
    public int Identification { get; set; }

    [Key(1)]
    public TIdentifier PrimaryId { get; set; }

    [Key(2)]
    public int NumberOfItems { get; set; }
}
