// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

public class EmptyFiler : IRawFiler
{
    public static readonly EmptyFiler Default = new();

    string IRawFiler.FilerPath
        => string.Empty;

    CrystalResult IRawFiler.Delete(string path)
        => CrystalResult.Success;

    Task<CrystalResult> IRawFiler.DeleteAllAsync()
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalResult> IRawFiler.DeleteAsync(string path, TimeSpan timeToWait)
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalResult> IRawFiler.PrepareAndCheck(StorageControl storage)
        => Task.FromResult(CrystalResult.Success);

    Task<CrystalMemoryOwnerResult> IRawFiler.ReadAsync(string path, long offset, int length, TimeSpan timeToWait)
        => Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.Success));

    Task IRawFiler.Terminate()
        => Task.CompletedTask;

    CrystalResult IRawFiler.Write(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
        => CrystalResult.Success;

    Task<CrystalResult> IRawFiler.WriteAsync(string path, long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait)
        => Task.FromResult(CrystalResult.Success);
}
