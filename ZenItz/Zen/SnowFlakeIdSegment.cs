// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public readonly struct SnowFlakeIdSegment
{
    public static bool IsValidSnowFlakeId(ulong snowFlakeId) => snowFlakeId != 0;

    public SnowFlakeIdSegment(ulong snowFlakeId, long segment)
    {
        this.SnowFlakeId = snowFlakeId;
        this.Segment = segment;
    }

    public bool IsValid => this.SnowFlakeId != 0;

    public uint SnowmanId => (uint)(this.SnowFlakeId >> 32);

    public uint FlakeId => (uint)this.SnowFlakeId;

    public int Offset => (int)(this.Segment >> 32);

    public int Count => (int)this.Segment;

    /// <summary>
    /// Gets Snowman id ((uint)(SnowFlakeId >> 32)) + Flake id ((uint)SnowFlakeId).<br/>
    /// 0: Unassigned.
    /// </summary>
    public readonly ulong SnowFlakeId;

    /// <summary>
    /// Gets a segment (offset: (int)(Segment >> 32), count: (int)(Segment)) of the flake.
    /// </summary>
    public readonly long Segment;
}
