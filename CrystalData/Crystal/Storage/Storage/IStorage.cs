﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Storage;

internal interface IStorage
{
    bool PrepareAndCheck(StorageControl storage);

    StorageResult Put(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared);

    StorageResult Delete(ref ulong fileId);

    Task<StorageMemoryOwnerResult> GetAsync(ulong fileId, TimeSpan timeToWait);

    Task<StorageResult> PutAsync(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared, TimeSpan timeToWait);

    Task<StorageResult> DeleteAsync(ref ulong fileId, TimeSpan timeToWait);

    /*Task<StorageMemoryOwnerResult> GetAsync(ulong fileId)
        => this.GetAsync(fileId, TimeSpan.MinValue);

    Task<StorageResult> PutAsync(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        => this.PutAsync(ref fileId, memoryToBeShared, TimeSpan.MinValue);

    Task<StorageResult> DeleteAsync(ref ulong fileId)
        => this.DeleteAsync(ref fileId, TimeSpan.MinValue);*/
}
