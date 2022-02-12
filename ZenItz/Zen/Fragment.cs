﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
internal partial class Fragment
{// by Yamamoto.
    public enum FragmentState
    {
        NotLoaded, // Not loaded
        NotSaved, // Active and not saved
        Saved, // Active and saved
    }

    internal Fragment()
    {// 1.For serializer 2.New primary
    }

    internal Fragment(Identifier secondaryId)
    {// 1.New secondary, lock (secondaryGoshujin)
        this.State = FragmentState.Saved;
        this.secondaryId = secondaryId;
        this.SnowFlakeId = SnowmanControl.Instance.GetFlakeId();
    }

    internal void Set(Flake primaryObject, ReadOnlySpan<byte> data)
    {// lock (secondaryGoshujin)
        if (this.himo != null && data.SequenceEqual(this.himo.MemoryOwner.Memory.Span))
        {// Identical
            return;
        }

        this.State = FragmentState.Loaded;
        this.himo = primaryObject.Zen.HimoControl.Create(in primaryObject.id, in this.secondaryId, data);
    }

    public FragmentState State { get; private set; }

    public Identifier SecondaryId => this.secondaryId;

    // Serialization & Link
    [Key(0)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    private Identifier secondaryId;

    /// <summary>
    /// Gets Snowman id ((uint)(SnowFlakeId >> 32)) + Flake id ((uint)SnowFlakeId).<br/>
    /// 0: Unassigned.
    /// </summary>
    [Key(1)]
    public ulong SnowFlakeId { get; private set; }

    /// <summary>
    /// Gets a segment (offset: (int)(Segment >> 32), count: (int)(Segment)) of the flake.
    /// </summary>
    [Key(2)]
    public long Segment { get; private set; }

    private Himo? himo;
}
