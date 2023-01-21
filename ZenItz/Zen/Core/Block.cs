// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public readonly struct Block
{
    internal Block(Zen zen, Flake.GoshujinClass goshujin)
    {
        this.Zen = zen;
        this.Goshujin = goshujin;
    }

    public readonly Zen Zen;

    internal readonly Flake.GoshujinClass Goshujin;

    public bool TryGetOrAddFlake(Identifier id, [MaybeNullWhen(false)] out Flake? flake)
    {
        if (!this.Zen.Started)
        {
            flake = null;
            return false;
        }

        lock (this.Goshujin)
        {
            if (!this.Goshujin.IdChain.TryGetValue(id, out flake))
            {
                flake = new Flake(this.Zen, id);
                this.Goshujin.Add(flake);
            }
        }

        return true;
    }

    public bool TryGetFlake(Identifier id, [MaybeNullWhen(false)] out Flake? flake)
    {
        if (!this.Zen.Started)
        {
            flake = null;
            return false;
        }

        lock (this.Goshujin)
        {
            return this.Goshujin.IdChain.TryGetValue(id, out flake);
        }
    }
}
