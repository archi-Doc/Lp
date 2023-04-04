// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
        => $"Id: {this.StorageId:x4} {this.Storage?.ToString()}";

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

    #endregion

    public async Task<CrystalResult> PrepareAndCheck(StorageGroup storageGroup, bool createNew)
    {
        var crystalizer = storageGroup.Crystalizer;

        if (this.Storage == null)
        {
            this.Storage = storageGroup.Crystalizer.ResolveStorage(this.StorageConfiguration);
        }

        var result = await this.Storage.PrepareAndCheck(crystalizer, this.StorageConfiguration, createNew).ConfigureAwait(false);
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
}
