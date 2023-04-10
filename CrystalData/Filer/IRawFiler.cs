// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;

namespace CrystalData.Filer;

public readonly struct FileInformation
{
    public FileInformation(string file, long length)
    {
        this.File = file;
        this.Length = length;
    }

    public readonly string File;
    public readonly long Length;
}

public interface IRawFiler
{
    bool SupportPartialWrite { get; }

    Task<CrystalResult> PrepareAndCheck(Crystalizer crystalizer, PathConfiguration configuration);

    Task Terminate();

    Task<CrystalMemoryOwnerResult> ReadAsync(string path, long offset, int length, TimeSpan timeToWait);

    CrystalResult Write(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, bool truncate = true);

    Task<CrystalResult> WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait, bool truncate = true);

    /// <summary>
    /// Delete the file matching the path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns><see cref="CrystalResult"/>.</returns>
    CrystalResult Delete(string path);

    Task<CrystalResult> DeleteAsync(string path, TimeSpan timeToWait);

    Task<List<FileInformation>> ListAsync(string path, string? pattern, TimeSpan timeToWait);

    Task<CrystalMemoryOwnerResult> ReadAsync(string path, long offset, int length)
        => this.ReadAsync(path, offset, length, TimeSpan.MinValue);

    Task<CrystalResult> WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, bool truncate = true)
        => this.WriteAsync(path, offset, dataToBeShared, TimeSpan.MinValue, truncate);

    Task<CrystalResult> DeleteAsync(string path)
        => this.DeleteAsync(path, TimeSpan.MinValue);

    Task<List<FileInformation>> ListAsync(string path, string? pattern)
        => this.ListAsync(path, pattern, TimeSpan.MinValue);
}
