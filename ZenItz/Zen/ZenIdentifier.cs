// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public readonly struct ZenIdentifier
{
    public static bool IsValidIO(ulong io) => io != 0;

    public ZenIdentifier(ulong io, long io2)
    {
        this.IO = io;
        this.IO2 = io2;
    }

    public bool IsValid => this.IO != 0;

    public uint DirectoryId => (uint)(this.IO >> 32);

    public uint SnowflakeId => (uint)this.IO;

    public int Offset => (int)(this.IO2 >> 32);

    public int Count => (int)this.IO2;

    /// <summary>
    /// Gets ZenDirectory id ((uint)(IO >> 32)) + Snowflake id ((uint)IO).<br/>
    /// 0: Unassigned.
    /// </summary>
    public readonly ulong IO;

    /// <summary>
    /// Gets a segment (offset: (int)(IO2 >> 32), count: (int)(IO2)) of the flake.
    /// </summary>
    public readonly long IO2;
}
