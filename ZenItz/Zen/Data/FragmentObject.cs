// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[ValueLinkObject]
internal partial class FragmentObject<TIdentifier> : MemoryObject
    where TIdentifier : IEquatable<TIdentifier>, IComparable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
    public FragmentObject(TIdentifier identifier)
        : base()
    {
        this.Identifier = identifier;
    }

    [Link(Primary = true, NoValue = true, Name = "Id", Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    public TIdentifier Identifier { get; private set; }
}
