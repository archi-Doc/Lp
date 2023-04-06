// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData;

public interface IFiler
{
    bool SupportPartialWrite { get; }

    /// <summary>
    /// This function may be called multiple times.
    /// </summary>
    /// <param name="crystalizer"><see cref="Crystalizer"/>.</param>
    /// <param name="configuration"><see cref="FilerConfiguration"/>.</param>
    /// <returns><see cref="CrystalResult"/>.</returns>
    Task<CrystalResult> PrepareAndCheck(Crystalizer crystalizer, FilerConfiguration configuration);

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
