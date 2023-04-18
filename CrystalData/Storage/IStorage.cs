// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public partial interface IStorage
{
    long StorageUsage { get; }

    void SetTimeout(TimeSpan timeout);

    Task<CrystalResult> PrepareAndCheck(PrepareParam param, StorageConfiguration storageConfiguration, bool createNew);

    Task Save();

    CrystalResult Put(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared);

    CrystalResult Delete(ref ulong fileId);

    Task<CrystalMemoryOwnerResult> GetAsync(ref ulong fileId);

    Task<CrystalResult> PutAsync(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared);

    Task<CrystalResult> DeleteAsync(ref ulong fileId);

    Task<CrystalResult> DeleteAllAsync();
}
