// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
public partial class Flake
{
    internal Flake()
    {
    }

    internal Flake(Zen zen, Identifier identifier)
    {
        this.Zen = zen;
        this.identifier = identifier;
    }

    public ZenResult Set(ReadOnlySpan<byte> data) => this.SetInternal(data, false);

    public async Task<(ZenResult Result, ByteArrayPool.MemoryOwner Value)> Get()
    {
        var tryLoad = false;
        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return (ZenResult.Removed, default);
            }

            if (this.primaryFragment != null)
            {
                return (ZenResult.Success, this.primaryFragment.MemoryOwner.IncrementAndShare());
            }
            else if (this.primarySnowFlakeId != 0)
            {
                tryLoad = true;
            }
        }

        if (tryLoad)
        {
            return this.Zen.SnowmanControl.TryLoadPrimary(this.Identifier);
        }

        return (ZenResult.NoData, default);
    }

    public ZenResult Set(Identifier fragmentId, ReadOnlySpan<byte> data)
    {
        if (data.Length > Zen.MaxSecondaryFragmentSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            this.secondaryFragment ??= new(this);
            return this.secondaryFragment.Set(fragmentId, data);
        }
    }

    public void Unload()
    {
    }

    public bool TryRemove() => this.Zen.TryRemove(this.Identifier);

    public Zen Zen { get; } = default!;

    public Identifier Identifier => this.identifier;

    public bool IsRemoved => this.Goshujin == null;

    public ZenResult SetInternal(ReadOnlySpan<byte> data, bool loading)
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

            if (!loading || this.primaryFragment == null)
            {// Not loading or Loading & empty
                this.primaryFragment ??= new(this);
                this.primaryFragment.Set(data, loading);
            }
        }

        return ZenResult.Success;
    }

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
    private SecondaryFragment? secondaryFragment;
}
