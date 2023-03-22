// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;
using CrystalData.Storage;

namespace CrystalData;

[TinyhandObject]
[ValueLinkObject]
internal partial class StorageAndFiler
{
    public StorageAndFiler()
    {
    }

    [IgnoreMember]
    public IStorage? Storage { get; set; }

    [IgnoreMember]
    public IFiler? Filer { get; set; }

    [Key(0)]
    [Link(Type = ChainType.Unordered, Primary = true, NoValue = true)]
    public ushort StorageId { get; set; }

    [Key(1)]
    public byte[] StorageData { get; set; } = default!;

    [Key(2)]
    public byte[] FilerData { get; set; } = default!;

    [Key(3)]
    public MemoryStat MemoryStat { get; private set; } = default!;

    public double GetUsageRatio()
    {
        if (this.Storage == null)
        {
            return 0d;
        }

        if (this.Storage.StorageCapacity == 0)
        {
            return 0d;
        }

        var ratio = (double)this.Storage.StorageUsage / this.Storage.StorageCapacity;
        if (ratio < 0)
        {
            ratio = 0;
        }
        else if (ratio > 1)
        {
            ratio = 1;
        }

        return ratio;
    }
}
