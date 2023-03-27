// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;
using CrystalData.Storage;

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

    #endregion

    public async Task<CrystalResult> PrepareAndCheck(StorageControl storageControl, bool newStorage)
    {
        if (this.Filer == null)
        {
            if (!TinyhandSerializer.TryDeserialize<IFiler>(this.FilerData, out var filer))
            {
                return CrystalResult.DeserializeError;
            }

            this.Filer = filer;
        }

        var result = await this.Filer.PrepareAndCheck(storageControl, newStorage).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return result;
        }

        if (this.Storage == null)
        {
            if (!TinyhandSerializer.TryDeserialize<IStorage>(this.StorageData, out var storage))
            {
                return CrystalResult.DeserializeError;
            }

            this.Storage = storage;
        }

        result = await this.Storage.PrepareAndCheck(storageControl, this.Filer, newStorage).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return result;
        }

        return CrystalResult.Success;
    }

    public void Start()
    {
    }

    public async Task Save()
    {
        if (this.Storage != null)
        {
            await this.Storage.Save();
        }
    }

    public async Task Terminate()
    {
        if (this.Filer != null)
        {
            await this.Filer.Terminate();
        }

        if (this.Filer != null)
        {
            this.FilerData = TinyhandSerializer.Serialize(this.Filer);
            this.Filer = null;
        }

        if (this.Storage != null)
        {
            this.StorageData = TinyhandSerializer.Serialize(this.Storage);
            this.Storage = null;
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
