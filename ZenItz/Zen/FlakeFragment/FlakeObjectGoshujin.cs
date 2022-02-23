// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal class FlakeObjectGoshujin
{
    public FlakeObjectGoshujin(Zen zen)
    {
        this.Zen = zen;
    }

    public Zen Zen { get; }

    public FlakeObjectBase.GoshujinClass Goshujin => this.goshujin;

    internal long TotalSize;

    private FlakeObjectBase.GoshujinClass goshujin = new();
}
