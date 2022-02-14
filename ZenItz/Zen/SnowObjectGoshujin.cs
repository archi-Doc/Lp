// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

internal partial class SnowObjectGoshujin
{
    public SnowObjectGoshujin(Zen zen)
    {
        this.Zen = zen;
    }

    public Zen Zen { get; }

    public SnowObject.GoshujinClass Goshujin => this.goshujin;

    private SnowObject.GoshujinClass goshujin = new();
}
