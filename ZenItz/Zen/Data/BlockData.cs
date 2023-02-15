// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public interface BlockData : IData
{
    const int Id = 1;

    static int IData.StaticId => Id;

    ZenResult Set(ReadOnlySpan<byte> data);

    ZenResult SetObject<T>(T obj)
            where T : ITinyhandSerialize<T>;

    Task<ZenMemoryResult> Get();

    Task<ZenObjectResult<T>> GetObject<T>()
            where T : ITinyhandSerialize<T>;
}

internal class BlockDataImpl : HimoGoshujinClass.Himo, BlockData, IBaseData
{
    public BlockDataImpl(IFlakeInternal flakeInternal)
        : base(flakeInternal)
    {
    }

    public override int Id => BlockData.Id;

    ZenResult BlockData.Set(ReadOnlySpan<byte> data)
    {
        if (data.Length > this.flakeInternal.Options.MaxDataSize)
        {
            return ZenResult.OverSizeLimit;
        }

        this.Update(this.memoryObject.SetSpanInternal(data), true);
        return ZenResult.Success;
    }

    ZenResult BlockData.SetObject<T>(T obj)
    {
        if (!FlakeFragmentService.TrySerialize(obj, out var memoryOwner))
        {
            return ZenResult.SerializeError;
        }
        else if (memoryOwner.Memory.Length > this.flakeInternal.Options.MaxDataSize)
        {
            return ZenResult.OverSizeLimit;
        }

        this.Update(this.memoryObject.SetMemoryOwnerInternal(memoryOwner.AsReadOnly(), obj), true);
        return ZenResult.Success;
    }

    async Task<ZenMemoryResult> BlockData.Get()
    {
        if (this.memoryObject.MemoryOwnerIsValid)
        {
            var memoryOwner = this.memoryObject.MemoryOwner.IncrementAndShare();
            this.Update();
            return new(ZenResult.Success, memoryOwner.Memory);
        }

        var result = await this.flakeInternal.StorageToData<BlockData>();
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

    async Task<ZenObjectResult<T>> BlockData.GetObject<T>()
    {
        if (this.memoryObject.MemoryOwnerIsValid &&
            this.memoryObject.TryGetObjectInternal(out T? obj) == ZenResult.Success)
        {
            return new(ZenResult.Success, obj);
        }

        var result = await this.flakeInternal.StorageToData<BlockData>();
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
    private void Update((bool Changed, int MemoryDifference) result, bool clearSavedFlag)
    {
        if (clearSavedFlag && result.Changed)
        {
            this.isSaved = false;
        }

        this.Update(result.MemoryDifference);
    }

    private bool isSaved = true;
    private MemoryObject memoryObject = new();

    void IBaseData.Save()
    {
        if (!this.isSaved)
        {// Not saved.
            this.flakeInternal.DataToStorage<BlockData>(this.memoryObject.MemoryOwner);
            this.isSaved = true;
        }
    }

    void IBaseData.Unload()
    {
        var memoryDifference = this.memoryObject.Clear();
        this.Remove(memoryDifference);
    }
}
