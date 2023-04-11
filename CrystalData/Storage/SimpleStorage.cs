// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using CrystalData.Filer;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1204

namespace CrystalData.Storage;

internal partial class SimpleStorage : IStorage
{
    private const string SimpleStorageMain = "Simple.main";

    public SimpleStorage()
    {
        this.timeout = TimeSpan.MinValue;
    }

    public override string ToString()
        => $"SimpleStorage {StorageHelper.ByteToString(this.StorageUsage)}";

    #region FieldAndProperty

    public long StorageUsage { get; private set; } // lock (this.syncObject)

    private string directory = string.Empty;
    private IFiler? filer;
    private IRawFiler? rawFiler;

    private object syncObject = new();
    private TimeSpan timeout;
    private Dictionary<uint, int> fileToSize = new();

    #endregion

    #region IStorage

    void IStorage.SetTimeout(TimeSpan timeout)
    {
        this.timeout = timeout;
    }

    async Task<CrystalResult> IStorage.PrepareAndCheck(Crystalizer crystalizer, StorageConfiguration storageConfiguration, bool createNew)
    {
        this.directory = storageConfiguration.DirectoryConfiguration.Path;
        if (!string.IsNullOrEmpty(this.directory) && !this.directory.EndsWith('/'))
        {
            this.directory += "/";
        }

        var filerConfiguration = storageConfiguration.DirectoryConfiguration.CombinePath(SimpleStorageMain);
        this.filer = crystalizer.ResolveFiler(filerConfiguration);
        var resultFiler = await this.filer.PrepareAndCheck(crystalizer, filerConfiguration).ConfigureAwait(false);
        if (resultFiler != CrystalResult.Success)
        {
            return resultFiler;
        }

        this.rawFiler = crystalizer.ResolveRawFiler(storageConfiguration.DirectoryConfiguration);
        resultFiler = await this.rawFiler.PrepareAndCheck(crystalizer, filerConfiguration).ConfigureAwait(false);
        if (resultFiler != CrystalResult.Success)
        {
            return resultFiler;
        }

        if (createNew)
        {
            return CrystalResult.Success;
        }

        var (result, waypoint) = await PathHelper.LoadData(this.filer).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result.Result;
        }

        try
        {
            if (TinyhandSerializer.Deserialize<Dictionary<uint, int>>(result.Data.Memory.Span) is { } dictionary)
            {
                this.fileToSize = dictionary;
            }
        }
        catch
        {
            return CrystalResult.DeserializeError;
        }
        finally
        {
            result.Return();
        }

        return CrystalResult.Success;
    }

    async Task IStorage.Save()
    {
        if (this.filer != null)
        {
            byte[] byteArray;
            lock (this.syncObject)
            {
                byteArray = TinyhandSerializer.Serialize(this.fileToSize);
            }

            await PathHelper.SaveData(byteArray, this.filer, 0);
        }
    }

    CrystalResult IStorage.Put(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {
        if (this.rawFiler == null)
        {
            return CrystalResult.NoFiler;
        }

        this.PutInternal(ref fileId, dataToBeShared.Memory.Length);
        return this.rawFiler.Write(this.FileToPath(FileIdToFile(fileId)), 0, dataToBeShared);
    }

    CrystalResult IStorage.Delete(ref ulong fileId)
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

        lock (this.syncObject)
        {
            if (!this.fileToSize.Remove(file))
            {// Not found
                fileId = 0;
                return CrystalResult.NoFile;
            }

            // this.dictionary.Add(file, -1);
        }

        fileId = 0;
        return this.rawFiler.Delete(this.FileToPath(file));
    }

    Task<CrystalMemoryOwnerResult> IStorage.GetAsync(ref ulong fileId)
    {
        if (this.rawFiler == null)
        {
            return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NoFiler));
        }

        var file = FileIdToFile(fileId);
        int size;
        lock (this.syncObject)
        {
            if (!this.fileToSize.TryGetValue(file, out size))
            {// Not found
                fileId = 0;
                return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NoFile));
            }
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
        if (this.rawFiler == null)
        {
            return Task.FromResult(CrystalResult.NoFiler);
        }

        var file = FileIdToFile(fileId);
        if (file == 0)
        {
            return Task.FromResult(CrystalResult.NoFile);
        }

        lock (this.syncObject)
        {
            this.fileToSize.Remove(file);
        }

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
        var file = FileIdToFile(fileId);
        lock (this.syncObject)
        {
            if (file != 0 && this.fileToSize.TryGetValue(file, out var size))
            {
                if (dataSize > size)
                {
                    this.StorageUsage += dataSize - size;
                }

                this.fileToSize[file] = dataSize;
            }
            else
            {// Not found
                file = this.NewFile(dataSize);
                this.StorageUsage += dataSize;
            }
        }

        fileId = FileToFileId(file);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NewFile(int size)
    {// lock (this.syncObject)
        while (true)
        {
            var file = RandomVault.Pseudo.NextUInt32();
            if (this.fileToSize.TryAdd(file, size))
            {
                return file;
            }
        }
    }

    #endregion
}
