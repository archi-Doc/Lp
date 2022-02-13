// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
public partial class Flake
{
    public enum FlakeState
    {
        NotSaved, // Active and not saved
        Saved, // Active and saved
        Removed, // Removed
    }

    internal Flake(Zen zen, Identifier identifier)
    {
        this.Zen = zen;
        this.identifier = identifier;
    }

    public ZenResult Set(ReadOnlySpan<byte> data)
    {
        if (data.Length > Zen.MaxPrimaryFragmentSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            this.fragment ??= new();
            this.fragment.Set(this, data);
        }

        return ZenResult.Success;
    }

    public ZenResult Set(Identifier fragmentId, ReadOnlySpan<byte> data)
    {
        if (data.Length > Zen.MaxSecondaryFragmentSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.fragmentGoshujin)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            if (!this.fragmentGoshujin.SecondaryIdChain.TryGetValue(fragmentId, out var secondary))
            {
                secondary = new Fragment(fragmentId);
            }

            secondary.Set(this, data);
        }

        return ZenResult.Success;
    }

    public void Unload()
    {
    }

    public bool TryRemove() => this.Zen.TryRemove(this.Identifier);

    public Zen Zen { get; }

    public FlakeState State { get; private set; }

    public Identifier Identifier => this.identifier;

    public bool IsRemoved => this.Goshujin == null;

    internal void CreateInternal(Flake.GoshujinClass goshujin)
    {// lock (flakeGoshujin)
        lock (this.syncObject)
        {
            if (this.Goshujin == null)
            {
                this.Goshujin = goshujin;
            }
        }
    }

    internal bool RemoveInternal()
    {// lock (flakeGoshujin)
        lock (this.syncObject)
        {
            if (this.Goshujin == null)
            {
                return false;
            }
            else
            {
                this.Goshujin = null;
                return true;
            }
        }
    }

    [Key(0)]
    [Link(Primary = true, Name = "Id", NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    internal Identifier identifier;

    /// <summary>
    /// Gets Snowman id ((uint)(SnowFlakeId >> 32)) + Flake id ((uint)SnowFlakeId).<br/>
    /// 0: Unassigned.
    /// </summary>
    [Key(1)]
    internal ulong primarySnowFlakeId;

    /// <summary>
    /// Gets a segment (offset: (int)(Segment >> 32), count: (int)(Segment)) of the flake.
    /// </summary>
    [Key(2)]
    internal long primarySegment;

    [Key(3)]
    internal ulong secondarySnowFlakeId;

    [Key(4)]
    internal long secondarySegment;

    private object syncObject = new();
    private PrimaryFragment? primaryFragment;
}
