﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

namespace CrystalData;

public interface IFiler
{
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

public interface IFiler<TData> : IFiler
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}

internal class RawFilerToFiler : IFiler
{
    public RawFilerToFiler(Crystalizer crystalizer, IRawFiler rawFiler, FilerConfiguration filerConfiguration)
    {
        this.Crystalizer = crystalizer;
        this.RawFiler = rawFiler;
        this.File = filerConfiguration.File;
    }

    public Crystalizer Crystalizer { get; }

    public IRawFiler RawFiler { get; }

    public string File { get; }

    CrystalResult IFiler.Delete()
        => this.RawFiler.Delete(this.File);

    Task<CrystalResult> IFiler.DeleteAsync(TimeSpan timeToWait)
        => this.RawFiler.DeleteAsync(this.File, timeToWait);

    Task<CrystalResult> IFiler.PrepareAndCheck(Crystalizer crystalizer, FilerConfiguration configuration)
         => this.RawFiler.PrepareAndCheck(crystalizer, configuration);

    Task<CrystalMemoryOwnerResult> IFiler.ReadAsync(long offset, int length, TimeSpan timeToWait)
        => this.RawFiler.ReadAsync(this.File, offset, length, timeToWait);

    CrystalResult IFiler.Write(long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
        => this.RawFiler.Write(this.File, offset, dataToBeShared);

    Task<CrystalResult> IFiler.WriteAsync(long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait)
        => this.RawFiler.WriteAsync(this.File, offset, dataToBeShared, timeToWait);
}