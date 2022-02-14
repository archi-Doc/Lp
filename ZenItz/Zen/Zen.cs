// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public class Zen
{
    public const int MaxFlakeSize = 1024 * 1024 * 4; // 4MB
    public const int MaxFragmentSize = 1024 * 4; // 4KB
    public const int MaxFragmentCount = 1000;
    public const long DefaultPrimarySizeLimit = 100_000_000; // 100MB

    public Zen()
    {
        this.SnowmanControl = new();
        this.FlakePool = new ByteArrayPool(MaxFlakeSize, (int)(DefaultPrimarySizeLimit / MaxFlakeSize));
        this.FragmentPool = new ByteArrayPool(MaxFragmentSize, 0);
        this.SnowFlakeGoshujin = new(this, this.FlakePool);
        this.SnowFragmentGoshujin = new(this, this.FragmentPool);
    }

    public Flake CreateOrGet(Identifier id)
    {
        Flake? flake;
        lock (this.flakeGoshujin)
        {
            if (!this.flakeGoshujin.IdChain.TryGetValue(id, out flake))
            {
                flake = new Flake(this, id);
                this.flakeGoshujin.Add(flake);
            }
            else
            {
                flake.CreateInternal(this.flakeGoshujin);
            }
        }

        return flake;
    }

    public Flake? TryGet(Identifier id)
    {
        Flake? flake;
        lock (this.flakeGoshujin)
        {
            this.flakeGoshujin.IdChain.TryGetValue(id, out flake);
            return flake;
        }
    }

    public bool TryRemove(Identifier id)
    {
        lock (this.flakeGoshujin)
        {
            if (this.flakeGoshujin.IdChain.TryGetValue(id, out var flake))
            {
                return flake.RemoveInternal();
            }
        }

        return false;
    }

    public SnowmanControl SnowmanControl { get; }

    public ByteArrayPool FlakePool { get; }

    public ByteArrayPool FragmentPool { get; }

    internal SnowObjectGoshujin SnowFlakeGoshujin;
    internal SnowObjectGoshujin SnowFragmentGoshujin;
    private Flake.GoshujinClass flakeGoshujin = new();
}
