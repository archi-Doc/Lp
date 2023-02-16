// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[TinyhandObject]
internal partial class StorageData
{
    public StorageData()
    {
    }

    [Key(0)]
    public ZenDirectory.GoshujinClass Directories { get; set; } = default!;

    [Key(1)]
    public Dictionary<int, MemoryStat> MemoryStats { get; set; } = default!;
}
