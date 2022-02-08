// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[ValueLinkObject]
public partial class PrimaryObject
{
    public enum PrimaryState
    {
        Loaded, // Active and not saved
        Saved, // Active and saved
        Removed, // Removed
    }

    internal PrimaryObject(Identifier primaryId)
    {
        this.PrimaryId = primaryId;
    }

    public async Task<ZenResult> Set(Identifier secondaryId, byte[] data)
    {
        lock (this.secondaryGoshujin)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            if (!this.secondaryGoshujin.SecondaryIdChain.TryGetValue(secondaryId, out var secondary))
            {
                secondary = new SecondaryObject(secondaryId);
            }

            secondary.Set(data);
        }

        // this.Flake.Set(this.PrimaryId, secondaryId, data);
        return ZenResult.Success;
    }

    public PrimaryState State { get; private set; }

    [Link(Primary = true, Type = ChainType.Ordered)] // Not unordered
    public Identifier PrimaryId { get; private set; }

    public bool IsRemoved => this.State == PrimaryState.Removed;

    private SecondaryObject.GoshujinClass secondaryGoshujin = new();
}
