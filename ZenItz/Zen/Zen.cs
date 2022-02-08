// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public class Zen
{
    public Zen(FlakeControl flakeControl, HimoControl himoControl)
    {
        this.FlakeControl = flakeControl;
        this.HimoControl = himoControl;
    }

    public PrimaryObject CreateOrGet(Identifier primaryId)
    {
        PrimaryObject? primary;
        lock (this.primaryGoshujin)
        {
            if (!this.primaryGoshujin.PrimaryIdChain.TryGetValue(primaryId, out primary))
            {
                primary = new PrimaryObject(this, primaryId);
                this.primaryGoshujin.Add(primary);
            }
        }

        return primary;
    }

    public PrimaryObject? TryGet(Identifier primaryId)
    {
        PrimaryObject? primary;
        lock (this.primaryGoshujin)
        {
            this.primaryGoshujin.PrimaryIdChain.TryGetValue(primaryId, out primary);
            return primary;
        }
    }

    public FlakeControl FlakeControl { get; }

    public HimoControl HimoControl { get; }

    private PrimaryObject.GoshujinClass primaryGoshujin = new();
}
