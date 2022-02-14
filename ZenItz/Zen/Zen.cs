// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public class Zen
{
    public const int MaxPrimaryFragmentSize = 1024 * 1024 * 4; // 4MB
    public const int MaxSecondaryFragmentSize = 1024 * 4; // 4KB
    public const int MaxSecondaryFragmentCount = MaxPrimaryFragmentSize / MaxSecondaryFragmentSize;
    public const long DefaultPrimarySizeLimit = 100_000_000; // 100MB

    public Zen()
    {
        this.SnowmanControl = new();
        this.SnowFlakeGoshujin = new(this);
        this.SnowFragmentGoshujin = new(this);
        this.PrimaryPool = new ByteArrayPool(BlockService.MaxBlockSize, (int)(DefaultPrimarySizeLimit / BlockService.MaxBlockSize));
        this.SecondaryPool = new ByteArrayPool(MaxSecondaryFragmentSize, 0);
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

    internal SnowObjectGoshujin SnowFlakeGoshujin;
    internal SnowObjectGoshujin SnowFragmentGoshujin;
    internal ByteArrayPool PrimaryPool;
    internal ByteArrayPool SecondaryPool;
    private Flake.GoshujinClass flakeGoshujin = new();
}
