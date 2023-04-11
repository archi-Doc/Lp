// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

public interface IRawFiler
{
    bool SupportPartialWrite { get; }

    /// <summary>
    /// Prepare the filer and check if the path is valid.<br/>
    /// This method may be called multiple times.
    /// </summary>
    /// <param name="crystalizer"><see cref="Crystalizer"/>.</param>
    /// <param name="configuration"><see cref="PathConfiguration"/>.</param>
    /// <returns><see cref="CrystalResult"/>.</returns>
    Task<CrystalResult> PrepareAndCheck(Crystalizer crystalizer, PathConfiguration configuration);

    Task Terminate();

    CrystalResult Write(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, bool truncate = true);

    /// <summary>
    /// Delete the file matching the path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns><see cref="CrystalResult"/>.</returns>
    CrystalResult Delete(string path);

    Task<CrystalMemoryOwnerResult> ReadAsync(string path, long offset, int length, TimeSpan timeout);

    Task<CrystalResult> WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeout, bool truncate = true);

    Task<CrystalResult> DeleteAsync(string path, TimeSpan timeout);

    Task<List<PathInformation>> ListAsync(string path, TimeSpan timeout);

    Task<CrystalMemoryOwnerResult> ReadAsync(string path, long offset, int length)
        => this.ReadAsync(path, offset, length, TimeSpan.MinValue);

    Task<CrystalResult> WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, bool truncate = true)
        => this.WriteAsync(path, offset, dataToBeShared, TimeSpan.MinValue, truncate);

    Task<CrystalResult> DeleteAsync(string path)
        => this.DeleteAsync(path, TimeSpan.MinValue);

    Task<List<PathInformation>> ListAsync(string path)
    => this.ListAsync(path, TimeSpan.MinValue);
}
