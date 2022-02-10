// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
public partial class Flake
{
    public enum FlakeState
    {
        NotSaved, // Active and not saved
        Saved, // Active and saved
        Removed, // Removed
    }

    internal Flake(Zen zen, Identifier id)
    {
        this.Zen = zen;
        this.id = id;
    }

    public ZenResult Set(Identifier secondaryId, ReadOnlySpan<byte> data)
    {
        if (data.Length > Zen.MaxSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.flakeGoshujin)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            if (!this.flakeGoshujin.SecondaryIdChain.TryGetValue(secondaryId, out var secondary))
            {
                secondary = new FlakeObject(secondaryId);
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

    public FlakeState State { get; private set; }

    public Identifier Id => this.id;

    public bool IsRemoved => this.State == FlakeState.Removed;

    [Link(Primary = true, NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    internal Identifier id;

    private FlakeObject? flakeObject;

    private FlakeObject.GoshujinClass flakeGoshujin = new();
}
