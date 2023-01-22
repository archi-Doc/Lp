// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class FragmentData<TIdentifier> : FlakeData<TIdentifier>
{
    public FragmentData(Zen<TIdentifier> zen, TIdentifier identifier)
        : base(zen)
    {
        this.TIdentifier = identifier;
    }

    [Link(Primary = true, NoValue = true, Name = "Id", Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    public TIdentifier TIdentifier { get; private set; }
}
