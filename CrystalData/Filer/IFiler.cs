// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

namespace CrystalData;

public interface IFiler
{
    bool SupportPartialWrite { get; }

    Task<CrystalResult> PrepareAndCheck(Crystalizer crystalizer, PathConfiguration configuration);

    CrystalResult Write(long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared);

    CrystalResult Delete();

    Task<CrystalMemoryOwnerResult> ReadAsync(long offset, int length, TimeSpan timeToWait);

    Task<CrystalMemoryOwnerResult> ReadAsync(long offset, int length)
        => this.ReadAsync(offset, length, TimeSpan.MinValue);

    Task<CrystalResult> WriteAsync(long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait);

    Task<CrystalResult> WriteAsync(long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
        => this.WriteAsync(offset, dataToBeShared, TimeSpan.MinValue);

    Task<CrystalResult> DeleteAsync(TimeSpan timeToWait);

    Task<CrystalResult> DeleteAsync()
        => this.DeleteAsync();
}
