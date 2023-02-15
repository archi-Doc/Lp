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

        this.Update(this.dualData.SetSpanInternal(data), true);
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

        this.Update(this.dualData.SetMemoryOwnerInternal(memoryOwner.AsReadOnly(), obj), true);
        return ZenResult.Success;
    }

    async Task<ZenMemoryResult> BlockData.Get()
    {
        if (this.dualData.MemoryOwnerIsValid)
        {
            var memoryOwner = this.dualData.MemoryOwner.IncrementAndShare();
            this.Update();
            return new(ZenResult.Success, memoryOwner.Memory);
        }

        var result = await this.flakeInternal.LoadInternal<BlockData>();
        if (result.IsSuccess)
        {
            this.Update(this.dualData.SetMemoryOwnerInternal(result.Data, null), false);
            return new(result.Result, result.Data.IncrementAndShare().Memory);
        }
        else
        {
            return new(result.Result);
        }
    }

    async Task<ZenObjectResult<T>> BlockData.GetObject<T>()
    {
        if (this.dualData.TryGetObjectInternal(out T? obj) == ZenResult.Success)
        {
            return new(ZenResult.Success, obj);
        }

        var result = await this.flakeInternal.LoadInternal<BlockData>();
        if (result.IsSuccess)
        {
            this.Update(this.dualData.SetMemoryOwnerInternal(result.Data, null), false);
            var objectResult = this.dualData.TryGetObjectInternal(out obj);
            return new(objectResult, obj);
        }
        else
        {
            return new(result.Result);
        }
    }

    public ZenResult TryGetObject<T>(out T? obj)
        where T : ITinyhandSerialize<T>
    {// using (Flake.semaphore)
        var result = this.dualData.TryGetObjectInternal(out obj);
        this.Update();
        return result;
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
    private MemoryOwnerAndObject dualData = new();

    public void Save()
    {
        if (!this.isSaved)
        {// Not saved.
            var memoryOwner = this.dualData.MemoryOwner.IncrementAndShare();
            this.flakeInternal.SaveInternal<BlockData>(memoryOwner);
            memoryOwner.Return();

            this.isSaved = true;
        }
    }

    public void Unload()
    {
        var memoryDifference = this.dualData.Clear();
        this.Remove(memoryDifference);
    }
}
