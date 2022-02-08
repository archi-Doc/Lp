// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[ValueLinkObject]
public partial class PrimaryObject
{
    internal PrimaryObject(Flake flake, Identifier primaryId)
    {
        this.Flake = flake;
        this.PrimaryId = primaryId;
    }

    public async Task<ZenResult> Set(Identifier secondaryId, byte[] data)
    {
        lock (this.secondaryGoshujin)
        {
            if (!this.secondaryGoshujin.SecondaryIdChain.TryGetValue(secondaryId, out var secondary))
            {
                secondary = new SecondaryObject(secondaryId);
            }

            secondary.Set(data);
        }

        // this.Flake.Set(this.PrimaryId, secondaryId, data);
        return ZenResult.Success;
    }

    public Flake Flake { get; protected set; }

    [Link(Primary = true, Type = ChainType.Ordered)]
    public Identifier PrimaryId { get; protected set; }

    private SecondaryObject.GoshujinClass secondaryGoshujin = new();
}
