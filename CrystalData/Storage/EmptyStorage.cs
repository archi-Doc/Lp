// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq.Expressions;

namespace CrystalData.Storage;

public partial class EmptyStorage : IStorageObsolete
{
    public static readonly EmptyStorage Default = new();

    long IStorageObsolete.StorageUsage => 0;

    void IStorageObsolete.SetTimeout(TimeSpan timeout)
        => Expression.Empty();

    Task<CrystalResult> IStorageObsolete.PrepareAndCheck(PrepareParam param, StorageConfiguration storageConfiguration, bool createNew)
        => Task.FromResult(CrystalResult.Success);

    Task IStorageObsolete.SaveStorage()
        => Task.CompletedTask;

    Task<CrystalMemoryOwnerResult> IStorageObsolete.GetAsync(ref ulong fileId)
        => Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.Success));

    CrystalResult IStorageObsolete.PutAndForget(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        => CrystalResult.Success;

    Task<CrystalResult> IStorageObsolete.PutAsync(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        => Task.FromResult(CrystalResult.Success);

    CrystalResult IStorageObsolete.DeleteAndForget(ref ulong fileId)
        => CrystalResult.Success;

    Task<CrystalResult> IStorageObsolete.DeleteAsync(ref ulong fileId)
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalResult> IStorageObsolete.DeleteStorageAsync()
        => Task.FromResult(CrystalResult.Success);
}
