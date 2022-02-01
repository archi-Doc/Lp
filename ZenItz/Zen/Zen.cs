// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public class Zen
{
    public Zen(ShipControl shipControl)
    {
        this.shipControl = shipControl;
    }

    public PrimaryObject CreateOrGet(Identifier primaryId, int sizeHint = 0)
    {
        PrimaryObject? primary;
        lock (this.primaryGoshujin)
        {
            if (!this.primaryGoshujin.PrimaryIdChain.TryGetValue(primaryId, out primary))
            {
                var shipId = this.shipControl.GetShipId(sizeHint);
                primary = new PrimaryObject(Identifier.One, shipId);
                this.primaryGoshujin.Add(primary);
            }
        }

        return primary;
    }

    private ShipControl shipControl;
    private PrimaryObject.GoshujinClass primaryGoshujin = new();
}
