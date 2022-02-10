// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public class Zen
{
    public const int MaxSize = 1024 * 1024 * 4; // 4MB

    public Zen()
    {
        this.FlakeControl = new();
        this.HimoControl = new(this);
    }

    public Flake CreateOrGet(Identifier id)
    {
        Flake? primary;
        lock (this.primaryGoshujin)
        {
            if (!this.primaryGoshujin.IdChain.TryGetValue(id, out primary))
            {
                primary = new Flake(this, id);
                this.primaryGoshujin.Add(primary);
            }
        }

        return primary;
    }

    public Flake? TryGet(Identifier primaryId)
    {
        Flake? primary;
        lock (this.primaryGoshujin)
        {
            this.primaryGoshujin.IdChain.TryGetValue(primaryId, out primary);
            return primary;
        }
    }

    public SnowmanControl FlakeControl { get; }

    public HimoControl HimoControl { get; }

    private Flake.GoshujinClass primaryGoshujin = new();
}
