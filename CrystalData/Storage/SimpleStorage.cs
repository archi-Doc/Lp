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

    private Crystalizer crystalizer;
    private string directory = string.Empty;
    private ICrystal<SimpleStorageData>? crystal;
    private SimpleStorageData? data;
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
        this.directory = storageConfiguration.DirectoryConfiguration.Path;
        if (!string.IsNullOrEmpty(this.directory) && !this.directory.EndsWith('/'))
        {
            this.directory += "/";
        }

        var filerConfiguration = storageConfiguration.DirectoryConfiguration.CombinePath(SimpleStorageFile);
        this.rawFiler = this.crystalizer.ResolveRawFiler(storageConfiguration.DirectoryConfiguration);
        var resultFiler = await this.rawFiler.PrepareAndCheck(this.crystalizer, filerConfiguration).ConfigureAwait(false);
        if (resultFiler != CrystalResult.Success)
        {
            return resultFiler;
        }

        if (this.crystal == null)
        {
            this.crystal = this.crystalizer.CreateCrystal<SimpleStorageData>();
            this.crystal.Configure(new(SavePolicy.Manual, filerConfiguration));
            var resultCrystal = await this.crystal.PrepareAndLoad(param).ConfigureAwait(false); // tempcode
            if (resultCrystal != CrystalStartResult.Success)
            {
                if (createNew)
                {// tempcode
                    return CrystalResult.Success;
                }

                return CrystalResult.NoData;
            }

            this.data = this.crystal.Object;
        }

        return CrystalResult.Success;
    }

    async Task IStorage.Save()
    {
        if (this.crystal != null)
        {
            await this.crystal.Save(true).ConfigureAwait(false);
        }
    }

    CrystalResult IStorage.Put(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {
        if (this.rawFiler == null)
        {
            return CrystalResult.NoFiler;
        }

        this.PutInternal(ref fileId, dataToBeShared.Memory.Length);
        return this.rawFiler.WriteAndForget(this.FileToPath(FileIdToFile(fileId)), 0, dataToBeShared);
    }

    CrystalResult IStorage.DeleteAndForget(ref ulong fileId)
    {
        if (this.rawFiler == null)
        {
            return CrystalResult.NoFiler;
        }

        var file = FileIdToFile(fileId);
        if (file == 0)
        {
            return CrystalResult.NoFile;
        }

        if (this.data != null)
        {
            if (this.data.Remove(file))
            {// Not found
                fileId = 0;
                return CrystalResult.NoFile;
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
            return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NoFiler));
        }

        var file = FileIdToFile(fileId);
        int size;
        if (!this.data.TryGetValue(file, out size))
        {// Not found
            fileId = 0;
            return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NoFile));
        }

        return this.rawFiler.ReadAsync(this.FileToPath(file), 0, size, this.timeout);
    }

    Task<CrystalResult> IStorage.PutAsync(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {
        if (this.rawFiler == null)
        {
            return Task.FromResult(CrystalResult.NoFiler);
        }

        this.PutInternal(ref fileId, dataToBeShared.Memory.Length);
        return this.rawFiler.WriteAsync(this.FileToPath(FileIdToFile(fileId)), 0, dataToBeShared, this.timeout);
    }

    Task<CrystalResult> IStorage.DeleteAsync(ref ulong fileId)
    {
        if (this.rawFiler == null || this.data == null)
        {
            return Task.FromResult(CrystalResult.NoFiler);
        }

        var file = FileIdToFile(fileId);
        if (file == 0)
        {
            return Task.FromResult(CrystalResult.NoFile);
        }

        this.data.Remove(file);

        fileId = 0;
        return this.rawFiler.DeleteAsync(this.FileToPath(file), this.timeout);
    }

    async Task<CrystalResult> IStorage.DeleteAllAsync()
    {
        if (this.rawFiler == null)
        {
            return CrystalResult.NoFiler;
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

    #region Internal

    private void PutInternal(ref ulong fileId, int dataSize)
    {
        if (this.data == null)
        {
            return;
        }

        var file = FileIdToFile(fileId);
        this.data.Put(file, dataSize);
        fileId = FileToFileId(file);
    }

    #endregion
}
