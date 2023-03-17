// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
internal partial class StorageData
{
    public StorageData()
    {
    }

    [Key(0)]
    public CrystalDirectory.GoshujinClass Directories { get; set; } = default!;

    [Key(1)]
    public Dictionary<ushort, MemoryStat> MemoryStats { get; set; } = default!;
}
