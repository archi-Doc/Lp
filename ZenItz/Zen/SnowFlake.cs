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
    [Link(Primary = true, NoValue = true, Type = ChainType.Unordered)]
    public uint SnowflakeId { get; private set; }

    [Key(1)]
    public int Position { get; internal set; }

    [Key(2)]
    public int Unused { get; internal set; }
}
