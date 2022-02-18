// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[TinyhandObject]
[ValueLinkObject]
internal partial class Snowflake
{
    public Snowflake()
    {
    }

    [Key(0)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint FlakeId { get; private set; }

    [Key(1)]
    public int Unused { get; private set; }
}
