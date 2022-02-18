// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal class FlakeObjectGoshujin
{
    public FlakeObjectGoshujin(Zen zen, ByteArrayPool pool)
    {
        this.Zen = zen;
        this.Pool = pool;
    }

    internal void Update(int diff)
    {// lock (this.goshujin)
    }

    public Zen Zen { get; }

    public ByteArrayPool Pool { get; }

    public FlakeObjectBase.GoshujinClass Goshujin => this.goshujin;

    internal long TotalSize;

    private FlakeObjectBase.GoshujinClass goshujin = new();
}
