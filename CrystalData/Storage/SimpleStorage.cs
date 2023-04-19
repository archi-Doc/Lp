// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using CrystalData.Filer;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1204

namespace CrystalData.Storage;

internal partial class SimpleStorage : IStorage
{
    private const string SimpleStorageFile = "Simple";

    public SimpleStorage(Crystalizer crystalizer)
    {
        this.crystalizer = crystalizer;
        this.timeout = TimeSpan.MinValue;
    }

    public override string ToString()
        => $"SimpleStorage {StorageHelper.ByteToString(this.StorageUsage)}";

    #region FieldAndProperty

    public long StorageUsage => this.data == null ? 0 : this.data.StorageUsage;

    private SimpleStorageData? data => this.crystal?.Object;

    private Crystalizer crystalizer;
    private string directory = string.Empty;
    private ICrystal<SimpleStorageData>? crystal;
    private IRawFiler? rawFiler;
    private TimeSpan timeout;

    #endregion

    #region IStorage

    void IStorage.SetTimeout(TimeSpan timeout)
    {
        this.timeout = timeout;
    }

    async Task<CrystalResult> IStorage.PrepareAndCheck(PrepareParam param, StorageConfiguration storageConfiguration, bool createNew)
    {
        CrystalResult result;
        var directoryConfiguration = storageConfiguration.DirectoryConfiguration;
        this.directory = directoryConfiguration.Path;
        if (!string.IsNullOrEmpty(this.directory) && !this.directory.EndsWith('/'))
        {
            this.directory += "/";
        }

        this.rawFiler = this.crystalizer.ResolveRawFiler(directoryConfiguration);
        result = await this.rawFiler.PrepareAndCheck(param, directoryConfiguration).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        if (this.crystal == null)
        {
            this.crystal = this.crystalizer.CreateCrystal<SimpleStorageData>();
            var filerConfiguration = directoryConfiguration.CombinePath(SimpleStorageFile);
            this.crystal.Configure(new(SavePolicy.Manual, filerConfiguration));

            result = await this.crystal.PrepareAndLoad(param).ConfigureAwait(false);
            return result;
        }

        return CrystalResult.Success;
    }

    async Task IStorage.Save()
    {
        if (this.crystal != null)
        {
            await this.crystal.Save().ConfigureAwait(false);
        }
    }

    CrystalResult IStorage.PutAndForget(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {
        if (this.rawFiler == null || this.data == null)
        {
            return CrystalResult.NotPrepared;
        }

        var file = FileIdToFile(fileId);
        this.data.Put(ref file, dataToBeShared.Memory.Length);
        fileId = FileToFileId(file);

        return this.rawFiler.WriteAndForget(this.FileToPath(FileIdToFile(fileId)), 0, dataToBeShared);
    }

    CrystalResult IStorage.DeleteAndForget(ref ulong fileId)
    {
        if (this.rawFiler == null)
        {
            return CrystalResult.NotPrepared;
        }

        var file = FileIdToFile(fileId);
        if (file == 0)
        {
            return CrystalResult.NotFound;
        }

        if (this.data != null)
        {
            if (this.data.Remove(file))
            {// Not found
                fileId = 0;
                return CrystalResult.NotFound;
            }

            // this.dictionary.Add(file, -1);
        }

        fileId = 0;
        return this.rawFiler.DeleteAndForget(this.FileToPath(file));
    }

    Task<CrystalMemoryOwnerResult> IStorage.GetAsync(ref ulong fileId)
    {
        if (this.rawFiler == null || this.data == null)
        {
            return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NotPrepared));
        }

        var file = FileIdToFile(fileId);
        int size;
        if (!this.data.TryGetValue(file, out size))
        {// Not found
            fileId = 0;
            return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NotFound));
        }

        return this.rawFiler.ReadAsync(this.FileToPath(file), 0, size, this.timeout);
    }

    Task<CrystalResult> IStorage.PutAsync(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {
        if (this.rawFiler == null || this.data == null)
        {
            return Task.FromResult(CrystalResult.NotPrepared);
        }

        var file = FileIdToFile(fileId);
        this.data.Put(ref file, dataToBeShared.Memory.Length);
        fileId = FileToFileId(file);

        return this.rawFiler.WriteAsync(this.FileToPath(FileIdToFile(fileId)), 0, dataToBeShared, this.timeout);
    }

    Task<CrystalResult> IStorage.DeleteAsync(ref ulong fileId)
    {
        if (this.rawFiler == null || this.data == null)
        {
            return Task.FromResult(CrystalResult.NotPrepared);
        }

        var file = FileIdToFile(fileId);
        if (file == 0)
        {
            return Task.FromResult(CrystalResult.NotFound);
        }

        this.data.Remove(file);

        fileId = 0;
        return this.rawFiler.DeleteAsync(this.FileToPath(file), this.timeout);
    }

    async Task<CrystalResult> IStorage.DeleteAllAsync()
    {
        if (this.rawFiler == null)
        {
            return CrystalResult.NotPrepared;
        }

        return await this.rawFiler.DeleteDirectoryAsync(this.directory).ConfigureAwait(false);
    }

    #endregion

    #region Helper

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint FileIdToFile(ulong fileId) => (uint)(fileId >> 32);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FileToFileId(uint file) => (ulong)file << 32;

    private string FileToPath(uint file)
    {
        Span<char> c = stackalloc char[9];
        var n = 0;

        c[n++] = UInt32ToChar(file >> 28);
        c[n++] = UInt32ToChar(file >> 24);

        c[n++] = '/';

        c[n++] = UInt32ToChar(file >> 20);
        c[n++] = UInt32ToChar(file >> 16);
        c[n++] = UInt32ToChar(file >> 12);
        c[n++] = UInt32ToChar(file >> 8);
        c[n++] = UInt32ToChar(file >> 4);
        c[n++] = UInt32ToChar(file);

        return this.directory + c.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char UInt32ToChar(uint x)
    {
        var a = x & 0xF;
        if (a < 10)
        {
            return (char)('0' + a);
        }
        else
        {
            return (char)('W' + a);
        }
    }

    #endregion
}
