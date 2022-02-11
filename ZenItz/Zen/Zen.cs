// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public class Zen
{
    public const int MaxFlakeSize = 1024 * 1024 * 4; // 4MB
    public const int MaxFragmentSize = 1024 * 4; // 4KB
    public const int MaxFragmentCount = MaxFlakeSize / MaxFragmentSize;

    public Zen()
    {
        this.SnowmanControl = new();
        this.HimoControl = new(this);
    }

    public Flake CreateOrGet(Identifier id)
    {
        Flake? primary;
        lock (this.flakeGoshujin)
        {
            if (!this.flakeGoshujin.IdChain.TryGetValue(id, out primary))
            {
                primary = new Flake(this, id);
                this.flakeGoshujin.Add(primary);
            }
        }

        return primary;
    }

    public Flake? TryGet(Identifier primaryId)
    {
        Flake? primary;
        lock (this.flakeGoshujin)
        {
            this.flakeGoshujin.IdChain.TryGetValue(primaryId, out primary);
            return primary;
        }
    }

    public SnowmanControl SnowmanControl { get; }

    public HimoControl HimoControl { get; }

    private Flake.GoshujinClass flakeGoshujin = new();
}
