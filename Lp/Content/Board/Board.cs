// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using ValueLink;

namespace Lp.Content;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record Board
{
    public const int MaxMessages = 1_000;

    public Board()
    {
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, AddValue = false)]
    public Identifier identifier { get; private set; }

    [Key(1)]
    public Message Description { get; private set; } = default!;

    [Key(2, Exclude = true)]
    public Message.GoshujinClass Messages { get; private set; } = default!;
}
