// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
public partial class PrimaryObject
{
    public enum PrimaryState
    {
        Loaded, // Active and not saved
        Saved, // Active and saved
        Removed, // Removed
    }

    internal PrimaryObject(Zen zen, Identifier primaryId)
    {
        this.Zen = zen;
        this.primaryId = primaryId;
    }

    public ZenResult Set(Identifier secondaryId, ReadOnlySpan<byte> data)
    {
        if (data.Length > Zen.MaxSize)
        {
            return ZenResult.OverSizeLimit;
        }

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

            secondary.Set(this, data);
        }

        // this.Flake.Set(this.PrimaryId, secondaryId, data);
        return ZenResult.Success;
    }

    public void Unload()
    {
    }

    public Zen Zen { get; }

    public PrimaryState State { get; private set; }

    public Identifier PrimaryId => this.primaryId;

    public bool IsRemoved => this.State == PrimaryState.Removed;

    [Link(Primary = true, Type = ChainType.Ordered)] // Not unordered
    internal Identifier primaryId;

    private SecondaryObject.GoshujinClass secondaryGoshujin = new();
}
