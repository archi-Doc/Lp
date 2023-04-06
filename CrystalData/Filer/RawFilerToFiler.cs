// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

internal class RawFilerToFiler : IFiler
{
    public RawFilerToFiler(Crystalizer crystalizer, IRawFiler rawFiler, FilerConfiguration filerConfiguration)
    {
        this.Crystalizer = crystalizer;
        this.RawFiler = rawFiler;
        // this.File = filerConfiguration.File;
        this.File = PathHelper.GetRootedFile(this.Crystalizer.RootDirectory, filerConfiguration.File);
    }

    public Crystalizer Crystalizer { get; }

    public IRawFiler RawFiler { get; }

    public string File { get; }

    bool IFiler.SupportPartialWrite => this.RawFiler.SupportPartialWrite;

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

    public override string ToString()
        => $"RawFilerToFile({this.RawFiler.ToString()}) Path:{this.File}";
}
