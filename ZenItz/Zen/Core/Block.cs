// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public readonly struct Block<TIdentifier>
{
    internal Block(Zen<TIdentifier> zen, Flake<TIdentifier>.GoshujinClass goshujin)
    {
        this.Zen = zen;
        this.Goshujin = goshujin;
    }

    public readonly Zen<TIdentifier> Zen;

    internal readonly Flake<TIdentifier>.GoshujinClass Goshujin;

    public bool TryGetOrAddFlake(TIdentifier id, [MaybeNullWhen(false)] out Flake<TIdentifier>? flake)
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
                flake = new Flake<TIdentifier>(this.Zen, id);
                this.Goshujin.Add(flake);
            }
        }

        return true;
    }

    public bool TryGetFlake(TIdentifier id, [MaybeNullWhen(false)] out Flake<TIdentifier>? flake)
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
