// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

internal class RawFilerToFiler : IFiler
{
    public RawFilerToFiler(Crystalizer crystalizer, IRawFiler rawFiler, PathConfiguration configuration)
    {
        this.Crystalizer = crystalizer;
        this.RawFiler = rawFiler;
        this.Path = configuration.Path;
        // this.File = PathHelper.GetRootedFile(this.Crystalizer.RootDirectory, filerConfiguration.Path);
    }

    public Crystalizer Crystalizer { get; }

    public IRawFiler RawFiler { get; }

    public string Path { get; }

    bool IFiler.SupportPartialWrite => this.RawFiler.SupportPartialWrite;

    CrystalResult IFiler.Delete()
        => this.RawFiler.Delete(this.Path);

    Task<CrystalResult> IFiler.DeleteAsync(TimeSpan timeToWait)
        => this.RawFiler.DeleteAsync(this.Path, timeToWait);

    Task<CrystalResult> IFiler.PrepareAndCheck(Crystalizer crystalizer, PathConfiguration configuration)
         => this.RawFiler.PrepareAndCheck(crystalizer, configuration);

    Task<CrystalMemoryOwnerResult> IFiler.ReadAsync(long offset, int length, TimeSpan timeToWait)
        => this.RawFiler.ReadAsync(this.Path, offset, length, timeToWait);

    CrystalResult IFiler.Write(long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, bool truncate)
        => this.RawFiler.Write(this.Path, offset, dataToBeShared, truncate);

    Task<CrystalResult> IFiler.WriteAsync(long offset, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared, TimeSpan timeToWait, bool truncate)
        => this.RawFiler.WriteAsync(this.Path, offset, dataToBeShared, timeToWait, truncate);

    public override string ToString()
        => $"RawFilerToFile({this.RawFiler.ToString()}) Path:{this.Path}";
}
