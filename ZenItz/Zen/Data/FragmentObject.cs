// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    [ValueLinkObject]
    internal partial class FragmentObject : MemoryObject
    {
        public FragmentObject(TIdentifier identifier)
            : base()
        {
            this.TIdentifier = identifier;
        }

        [Link(Primary = true, NoValue = true, Name = "Id", Type = ChainType.Unordered)]
        [Link(Name = "OrderedId", Type = ChainType.Ordered)]
        public TIdentifier TIdentifier { get; private set; }
    }
}
