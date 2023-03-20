// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

internal interface IFiler
{
    StorageResult Write(string path, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared);

    StorageResult Delete(string path);

    Task<StorageMemoryOwnerResult> ReadAsync(string path, int sizeToRead, TimeSpan timeToWait);

    Task<StorageResult> WriteAsync(string path, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait);

    Task<StorageResult> DeleteAsync(string path, TimeSpan timeToWait);

    /*Task<StorageMemoryOwnerResult> ReadAsync(string path, int sizeToRead)
        => this.ReadAsync(path, sizeToRead, TimeSpan.MinValue);

    Task<StorageResult> WriteAsync(string path, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
        => this.WriteAsync(path, dataToBeShared, TimeSpan.MinValue);

    Task<StorageResult> DeleteAsync(string path)
        => this.DeleteAsync(path, TimeSpan.MinValue);*/
}
