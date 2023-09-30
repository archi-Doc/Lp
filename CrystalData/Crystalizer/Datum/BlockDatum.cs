// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData.Datum;

public interface BlockDatum : IDatum
{
    CrystalResult Set(ReadOnlySpan<byte> data);

    CrystalResult SetObject<T>(T obj)
            where T : ITinyhandSerialize<T>;

    Task<CrystalMemoryResult> Get();

    Task<CrystalObjectResult<T>> GetObject<T>()
            where T : ITinyhandSerialize<T>;
}

public class BlockDatumImpl : HimoGoshujinClass.Himo, BlockDatum, IBaseDatum
{
    public BlockDatumImpl(IDataInternal flakeInternal)
        : base(flakeInternal)
    {
    }

    CrystalResult BlockDatum.Set(ReadOnlySpan<byte> data)
    {
        if (data.Length > this.dataInternal.BigCrystal.BigCrystalConfiguration.MaxDataSize)
        {
            return CrystalResult.OverSizeLimit;
        }

        this.Update(this.memoryObject.SetSpanInternal(data), true);
        return CrystalResult.Success;
    }

    CrystalResult BlockDatum.SetObject<T>(T obj)
    {
        if (!SerializeHelper.TrySerialize(obj, out var memoryOwner))
        {
            return CrystalResult.SerializeError;
        }
        else if (memoryOwner.Memory.Length > this.dataInternal.BigCrystalConfiguration.MaxDataSize)
        {
            memoryOwner.Return();
            return CrystalResult.OverSizeLimit;
        }

        this.Update(this.memoryObject.SetMemoryOwnerInternal(memoryOwner.AsReadOnly(), obj), true);
        return CrystalResult.Success;
    }

    async Task<CrystalMemoryResult> BlockDatum.Get()
    {
        if (this.memoryObject.MemoryOwnerIsValid)
        {
            var memoryOwner = this.memoryObject.MemoryOwner.IncrementAndShare();
            this.UpdateHimo();
            return new(CrystalResult.Success, memoryOwner.Memory);
        }

        var result = await this.dataInternal.StorageToDatum<BlockDatum>().ConfigureAwait(false);
        if (result.IsSuccess)
        {
            this.Update(this.memoryObject.SetMemoryOwnerInternal(result.Data, null), false);
            return new(result.Result, result.Data.IncrementAndShare().Memory);
        }
        else
        {
            return new(result.Result);
        }
    }

    async Task<CrystalObjectResult<T>> BlockDatum.GetObject<T>()
    {
        if (this.memoryObject.MemoryOwnerIsValid &&
            this.memoryObject.TryGetObjectInternal(out T? obj) == CrystalResult.Success)
        {
            return new(CrystalResult.Success, obj);
        }

        var result = await this.dataInternal.StorageToDatum<BlockDatum>().ConfigureAwait(false);
        if (result.IsSuccess)
        {
            this.Update(this.memoryObject.SetMemoryOwnerInternal(result.Data, null), false);
            var objectResult = this.memoryObject.TryGetObjectInternal(out obj);
            return new(objectResult, obj);
        }
        else
        {
            return new(result.Result);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update((bool Changed, int NewSize) result, bool clearSavedFlag)
    {
        if (clearSavedFlag && result.Changed)
        {
            this.isSaved = false;
        }

        this.UpdateHimo(result.NewSize);
    }

    private bool isSaved = true;
    private MemoryObject memoryObject = new();

    void IBaseDatum.Save()
    {
        if (!this.isSaved)
        {// Not saved.
            this.dataInternal.DatumToStorage<BlockDatum>(this.memoryObject.MemoryOwner);
            this.isSaved = true;
        }
    }

    void IBaseDatum.Unload()
    {
        this.memoryObject.Clear();
        this.RemoveHimo();
    }
}
