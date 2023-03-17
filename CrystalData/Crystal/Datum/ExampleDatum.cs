// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData.Datum;

public interface ExampleDatum : IDatum
{
    const ushort Id = 0;

    static ushort IDatum.StaticId => Id;

    CrystalResult Set(ReadOnlySpan<byte> data);

    Task<CrystalMemoryResult> Get();
}

public class ExampleDatumImpl : HimoGoshujinClass.Himo, ExampleDatum, IBaseDatum
{
    public ExampleDatumImpl(IDataInternal flakeInternal)
        : base(flakeInternal)
    {
    }

    public override ushort Id => BlockDatum.Id;

    CrystalResult ExampleDatum.Set(ReadOnlySpan<byte> data)
    {
        if (this.byteArray != null &&
            this.byteArray.AsSpan().SequenceEqual(data))
        {// Identical
            this.Update(data.Length, false, false);
            return CrystalResult.Success;
        }

        this.byteArray = new byte[data.Length];
        data.CopyTo(this.byteArray);
        this.Update(data.Length, false, true);
        return CrystalResult.Success;
    }

    async Task<CrystalMemoryResult> ExampleDatum.Get()
    {
        if (this.byteArray != null)
        {
            this.UpdateHimo();
            return new(CrystalResult.Success, this.byteArray.AsMemory());
        }

        var result = await this.dataInternal.StorageToDatum<ExampleDatum>().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return new(result.Result);
        }

        var memory = result.Data.Memory;
        this.Update(memory.Length, true, false);
        return new(result.Result, memory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(int size, bool changed, bool clearSavedFlag)
    {
        if (clearSavedFlag && changed)
        {
            this.isSaved = false;
        }

        this.UpdateHimo(size);
    }

    private bool isSaved = true;
    private byte[]? byteArray;

    void IBaseDatum.Save()
    {
        if (!this.isSaved)
        {// Not saved.
            if (this.byteArray != null)
            {
                this.dataInternal.DataToStorage<ExampleDatum>(new(this.byteArray));
            }

            this.isSaved = true;
        }
    }

    void IBaseDatum.Unload()
    {
        this.byteArray = null;
        this.RemoveHimo();
    }
}
