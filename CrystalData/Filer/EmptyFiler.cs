// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

public partial class EmptyFiler : IRawFiler
{
    public static readonly EmptyFiler Default = new();

    bool IRawFiler.SupportPartialWrite => true;

    CrystalResult IRawFiler.Delete(string path)
        => CrystalResult.Success;

    Task<CrystalResult> IRawFiler.DeleteAsync(string path, TimeSpan timeout)
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalResult> IRawFiler.DeleteDirectoryAsync(string path, TimeSpan timeout)
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalResult> IRawFiler.PrepareAndCheck(Crystalizer crystalizer, PathConfiguration configuration)
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalMemoryOwnerResult> IRawFiler.ReadAsync(string path, long offset, int length, TimeSpan timeout)
        => Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NoFile));

    Task IRawFiler.Terminate()
        => Task.CompletedTask;

    CrystalResult IRawFiler.Write(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, bool truncate)
        => CrystalResult.Success;

    Task<CrystalResult> IRawFiler.WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeout, bool truncate)
        => Task.FromResult(CrystalResult.Success);

    Task<List<PathInformation>> IRawFiler.ListAsync(string path, TimeSpan timeout)
        => Task.FromResult(new List<PathInformation>());
}
