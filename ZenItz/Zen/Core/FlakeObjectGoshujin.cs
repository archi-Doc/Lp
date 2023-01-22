// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal class FlakeObjectGoshujin<TIdentifier>
{
    public FlakeObjectGoshujin(Zen<TIdentifier> zen)
    {
        this.Zen = zen;
    }

    public Zen<TIdentifier> Zen { get; }

    public FlakeObjectBase<TIdentifier>.GoshujinClass Goshujin => this.goshujin;

    internal long TotalSize;

    private FlakeObjectBase<TIdentifier>.GoshujinClass goshujin = new();
}
