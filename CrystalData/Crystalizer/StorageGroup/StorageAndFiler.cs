// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData;

[TinyhandObject]
[ValueLinkObject]
internal partial class StorageAndFiler
{
    public StorageAndFiler()
    {
    }

    public override string ToString()
        => $"Id: {this.StorageId:x4} {this.Storage?.ToString()} {this.Filer?.ToString()}";

    #region FieldAndProperty

    [IgnoreMember]
    public IStorage? Storage { get; set; }

    [IgnoreMember]
    public IRawFiler? Filer { get; set; }

    [Key(0)]
    [Link(Type = ChainType.Unordered, Primary = true, NoValue = true)]
    public ushort StorageId { get; set; }

    [Key(1)]
    public StorageConfiguration StorageConfiguration { get; set; } = default!;

    [Key(2)]
    public FilerConfiguration FilerConfiguration { get; set; } = default!;

    [Key(3)]
    public MemoryStat MemoryStat { get; private set; } = default!;

    #endregion

    public async Task<CrystalResult> PrepareAndCheck(StorageGroup storageGroup, bool newStorage)
    {
        var crystalizer = storageGroup.Crystalizer;

        if (this.Filer == null)
        {
            this.Filer = crystalizer.ResolveRawFiler(this.FilerConfiguration);
        }

        var result = await this.Filer.PrepareAndCheck(crystalizer, this.FilerConfiguration).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return result;
        }

        if (this.Storage == null)
        {
            this.Storage = storageGroup.Crystalizer.ResolveStorage(this.StorageConfiguration);
        }

        result = await this.Storage.PrepareAndCheck(this.Filer, newStorage).ConfigureAwait(false);
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
