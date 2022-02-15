// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class Fragment2 : Fragment
{
    public Fragment2(Zen zen, Identifier identifier)
        : base(zen)
    {
        this.Identifier = identifier;
    }

    [Link(Primary = true, NoValue = true, Name = "Id", Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    public Identifier Identifier { get; private set; }
}
