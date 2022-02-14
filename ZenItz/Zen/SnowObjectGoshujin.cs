// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

internal class SnowObjectGoshujin
{
    public SnowObjectGoshujin(Zen zen, ByteArrayPool pool)
    {
        this.Zen = zen;
        this.Pool = pool;
    }

    public Zen Zen { get; }

    public ByteArrayPool Pool { get; }

    public SnowObject.GoshujinClass Goshujin => this.goshujin;

    private SnowObject.GoshujinClass goshujin = new();
}
