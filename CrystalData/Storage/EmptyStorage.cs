// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

namespace CrystalData.Storage;

public partial class EmptyStorage : IStorage
{
    public static readonly EmptyStorage Default = new();

    long IStorage.StorageCapacity { get; set; }

    long IStorage.StorageUsage => 0;

    CrystalResult IStorage.Delete(ref ulong fileId)
        => CrystalResult.Success;

    Task<CrystalResult> IStorage.DeleteAsync(ref ulong fileId, TimeSpan timeToWait)
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalMemoryOwnerResult> IStorage.GetAsync(ref ulong fileId, TimeSpan timeToWait)
        => Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.Success));

    Task<CrystalResult> IStorage.PrepareAndCheck(IRawFiler filer, bool newStorage)
        => Task.FromResult(CrystalResult.Success);

    CrystalResult IStorage.Put(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        => CrystalResult.Success;

    Task<CrystalResult> IStorage.PutAsync(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared, TimeSpan timeToWait)
        => Task.FromResult(CrystalResult.Success);

    Task IStorage.Save()
        => Task.CompletedTask;
}
