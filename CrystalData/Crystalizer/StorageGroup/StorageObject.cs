// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData;

[TinyhandObject]
[ValueLinkObject]
internal partial class StorageObject
{
    public StorageObject()
    {
    }

    public StorageObject(ushort storageId, StorageConfiguration storageConfiguration)
    {
        this.StorageId = storageId;
        this.StorageConfiguration = storageConfiguration;
        this.MemoryStat = TinyhandSerializer.Reconstruct<MemoryStat>();
    }

    public override string ToString()
        => $"Id: {this.StorageId:x4} {this.Storage?.ToString()} ({this.StorageConfiguration.DirectoryConfiguration.ToString()})";

    #region FieldAndProperty

    [IgnoreMember]
    public IStorage? Storage { get; set; }

    [Key(0)]
    [Link(Type = ChainType.Unordered, Primary = true, NoValue = true)]
    public ushort StorageId { get; set; }

    [Key(1)]
    public StorageConfiguration StorageConfiguration { get; set; } = default!;

    [Key(2)]
    public MemoryStat MemoryStat { get; private set; } = default!;

    [Key(3)]
    public long StorageCapacity { get; set; } = StorageGroup.DefaultStorageCapacity;

    #endregion

    public async Task<CrystalResult> PrepareAndCheck(StorageGroup storageGroup, PrepareParam param, bool createNew)
    {
        var crystalizer = storageGroup.Crystalizer;

        if (this.Storage == null)
        {
            this.Storage = storageGroup.Crystalizer.ResolveStorage(this.StorageConfiguration);
        }

        var result = await this.Storage.PrepareAndCheck(param, this.StorageConfiguration, createNew).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return result;
        }

        return CrystalResult.Success;
    }

    public async Task Save()
    {
        if (this.Storage != null)
        {
            await this.Storage.Save();
        }
    }

    internal double GetUsageRatio()
    {
        if (this.Storage is not { } storage)
        {
            return 0d;
        }

        if (this.StorageCapacity == 0)
        {
            return 0d;
        }

        var ratio = (double)storage.StorageUsage / this.StorageCapacity;
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
