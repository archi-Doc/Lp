// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public class Zen
{
    public Zen(FlakeControl shipControl)
    {
        this.flakeControl = shipControl;
    }

    public PrimaryObject CreateOrGet(Identifier primaryId)
    {
        PrimaryObject? primary;
        lock (this.primaryGoshujin)
        {
            if (!this.primaryGoshujin.PrimaryIdChain.TryGetValue(primaryId, out primary))
            {
                var flake = this.flakeControl.GetFlake();
                primary = new PrimaryObject(flake, primaryId);
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

    private FlakeControl flakeControl;
    private PrimaryObject.GoshujinClass primaryGoshujin = new();
}
