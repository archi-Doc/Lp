// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

public partial class EmptyFiler : IRawFiler
{
    public static readonly EmptyFiler Default = new();

    bool IRawFiler.SupportPartialWrite => true;

    CrystalResult IRawFiler.Delete(string path)
        => CrystalResult.Success;

    Task<CrystalResult> IRawFiler.DeleteAsync(string path, TimeSpan timeToWait)
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalResult> IRawFiler.PrepareAndCheck(Crystalizer crystalizer, PathConfiguration configuration)
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalMemoryOwnerResult> IRawFiler.ReadAsync(string path, long offset, int length, TimeSpan timeToWait)
        => Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.Success));

    Task IRawFiler.Terminate()
        => Task.CompletedTask;

    CrystalResult IRawFiler.Write(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, bool truncate)
        => CrystalResult.Success;

    Task<CrystalResult> IRawFiler.WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait, bool truncate)
        => Task.FromResult(CrystalResult.Success);
}
