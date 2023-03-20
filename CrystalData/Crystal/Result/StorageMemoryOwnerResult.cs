// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public readonly struct StorageMemoryOwnerResult
{
    public StorageMemoryOwnerResult(StorageResult result, ByteArrayPool.MemoryOwner data)
    {
        this.Result = result;
        this.Data = data;
    }

    public StorageMemoryOwnerResult(StorageResult result)
    {
        this.Result = result;
        this.Data = default;
    }

    public void Return() => this.Data.Return();

    public bool IsSuccess => this.Result == StorageResult.Success;

    public readonly StorageResult Result;

    public readonly ByteArrayPool.MemoryOwner Data;
}
