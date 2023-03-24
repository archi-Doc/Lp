// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

namespace CrystalData.Storage;

[TinyhandUnion(0, typeof(SimpleStorage))]
internal partial interface IStorage
{
    long StorageCapacity { get; set; }

    long StorageUsage { get; }

    Task<CrystalResult> PrepareAndCheck(StorageControl storage, IFiler filer);

    Task Save(bool stop);

    CrystalResult Put(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared);

    CrystalResult Delete(ref ulong fileId);

    Task<CrystalMemoryOwnerResult> GetAsync(ref ulong fileId, TimeSpan timeToWait);

    Task<CrystalResult> PutAsync(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared, TimeSpan timeToWait);

    Task<CrystalResult> DeleteAsync(ref ulong fileId, TimeSpan timeToWait);

    /*Task<CrystalMemoryOwnerResult> GetAsync(ulong fileId)
        => this.GetAsync(fileId, TimeSpan.MinValue);

    Task<CrystalResult> PutAsync(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        => this.PutAsync(ref fileId, memoryToBeShared, TimeSpan.MinValue);

    Task<CrystalResult> DeleteAsync(ref ulong fileId)
        => this.DeleteAsync(ref fileId, TimeSpan.MinValue);*/
}
