// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IStorage
{
    long StorageUsage { get; }

    void SetTimeout(TimeSpan timeout);

    Task<CrystalResult> PrepareAndCheck(PrepareParam param, StorageConfiguration storageConfiguration, bool createNew);

    Task SaveStorage();

    Task<CrystalMemoryOwnerResult> GetAsync(ref StorageId storageId);

    CrystalResult PutAndForget(ref StorageId storageId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared);

    Task<CrystalResult> PutAsync(ref StorageId storageId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared);

    CrystalResult DeleteAndForget(ref StorageId storageId);

    Task<CrystalResult> DeleteAsync(ref StorageId storageId);

    Task<CrystalResult> DeleteStorageAsync();
}
