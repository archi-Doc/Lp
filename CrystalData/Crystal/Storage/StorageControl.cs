// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Results;
using CrystalData.Storage;

namespace CrystalData;

public sealed class StorageControl
{
    public const int DirectoryRotationThreshold = 1024 * 1024 * 100; // 100 MB

    internal StorageControl(UnitLogger unitLogger)
    {
        this.UnitLogger = unitLogger;
    }

    public CrystalDirectoryInformation[] GetDirectoryInformation()
    {
        lock (this.syncObject)
        {
            return Array.Empty<CrystalDirectoryInformation>();
            // return this.data.Directories.Select(a => a.GetInformation()).ToArray();
        }
    }

    public AddDictionaryResult AddDirectory(string path, ushort id = 0, long capacity = CrystalOptions.DefaultDirectoryCapacity)
    {
        if (capacity < 0)
        {
            capacity = CrystalOptions.DefaultDirectoryCapacity;
        }

        if (path.EndsWith('\\'))
        {
            path = path.Substring(0, path.Length - 1);
        }

        if (File.Exists(path))
        {
            return AddDictionaryResult.FileExists;
        }

        var relative = Path.GetRelativePath(this.Options.RootPath, path);
        if (!relative.StartsWith("..\\"))
        {
            path = relative;
        }

        lock (this.syncObject)
        {
            if (id == 0)
            {
                id = this.GetFreeDirectoryId(this.data.Directories);
            }

            var directory = new CrystalDirectory(id, path);
            directory.DirectoryCapacity = capacity;

            if (this.data.Directories.DirectoryIdChain.ContainsKey(id))
            {
                return AddDictionaryResult.DuplicateId;
            }
            else if (this.data.Directories.DirectoryPathChain.ContainsKey(path))
            {
                return AddDictionaryResult.DuplicatePath;
            }

            this.data.Directories.Add(directory);
        }

        return AddDictionaryResult.Success;
    }

    public void DeleteAll()
    {
        lock (this.syncObject)
        {
            foreach (var x in this.storageObjects)
            {
                x.Filer?.DeleteAll();
            }
        }
    }

    public CrystalOptions Options { get; private set; } = CrystalOptions.Default;

    public bool Started { get; private set; }

    public void Save(ref ushort storageId, ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared, ushort datumId)
    {
        StorageAndFiler? storage;
        lock (this.syncObject)
        {
            if (this.storageObjects.StorageIdChain.Count == 0)
            {// No storage available.
                return;
            }
            else if (storageId == 0 || !this.storageObjects.StorageIdChain.TryGetValue(storageId, out storage))
            {// Get valid directory.
                if (this.storageRotationCount >= DirectoryRotationThreshold ||
                    this.currentStorage == null)
                {
                    this.currentStorage = this.GetValidStorage();
                    this.storageRotationCount = memoryToBeShared.Memory.Length;
                    if (this.currentStorage == null)
                    {
                        return;
                    }
                }
                else
                {
                    this.storageRotationCount += memoryToBeShared.Memory.Length;
                }

                storage = this.currentStorage;
                storageId = storage.StorageId;
            }

            storage.MemoryStat.Add(memoryToBeShared.Memory.Length);
        }

        storage.Storage?.Put(ref fileId, memoryToBeShared);
    }

    public Task<CrystalMemoryOwnerResult> Load(ushort storageId, ulong fileId)
    {
        StorageAndFiler? storageObject;
        lock (this.syncObject)
        {
            storageObject = this.GetStorageFromId(storageId);
            if (storageObject == null)
            {
                return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NoDirectory));
            }
        }

        var task = storageObject.Storage?.GetAsync(ref fileId, TimeSpan.MinValue);
        return .ContinueWith()
    }

    public void Delete(ref ushort storageId, ref ulong fileId)
    {
        StorageAndFiler? storageObject;
        lock (this.syncObject)
        {
            storageObject = this.GetStorageFromId(storageId);
            if (storageObject == null)
            {// No storage
                storageId = 0;
                fileId = 0;
                return;
            }
        }

        storageObject.Storage?.Delete(ref fileId);
    }

    internal async Task<CrystalStartResult> TryStart(CrystalOptions options, CrystalStartParam param, ReadOnlyMemory<byte>? data)
    {// semaphore
        if (this.Started)
        {
            return CrystalStartResult.Success;
        }

        this.Options = options;

        StorageAndFiler.GoshujinClass? goshujin = null;
        if (data != null)
        {
            try
            {
                goshujin = TinyhandSerializer.Deserialize<StorageAndFiler.GoshujinClass>(data.Value);
            }
            catch
            {
                if (!await param.Query(CrystalStartResult.FileError).ConfigureAwait(false))
                {
                    return CrystalStartResult.FileError;
                }
            }
        }

        goshujin ??= TinyhandSerializer.Reconstruct<StorageAndFiler.GoshujinClass>();
        List<string>? errorDirectories = null;
        foreach (var x in goshujin.StorageIdChain)
        {
            if (await x.Storage?.PrepareAndCheck(this, ) != StorageResult.Success)
            {
                errorDirectories ??= new();
                errorDirectories.Add(x.DirectoryPath);
            }
        }

        if (errorDirectories != null &&
            !await param.Query(CrystalStartResult.DirectoryError, errorDirectories.ToArray()).ConfigureAwait(false))
        {
            return CrystalStartResult.FileError;
        }

        if (goshujin.Directories.DirectoryIdChain.Count == 0)
        {
            try
            {
                var defaultDirectory = new CrystalDirectory(this.GetFreeDirectoryId(goshujin.Directories), PathHelper.GetRootedDirectory(this.Options.RootPath, this.Options.DefaultCrystalDirectory));
                defaultDirectory.PrepareAndCheck(this);
                goshujin.Directories.Add(defaultDirectory);
            }
            catch
            {
            }
        }

        foreach (var x in goshujin.Directories)
        {
            x.Start();
        }

        if (goshujin.Directories.DirectoryIdChain.Count == 0)
        {
            return CrystalStartResult.NoDirectoryAvailable;
        }

        lock (this.syncObject)
        {
            this.ClearGoshujin();
            this.data = goshujin;
            this.currentDirectory = null;
            this.Started = true;
        }

        return CrystalStartResult.Success;
    }

    internal async Task StopAsync()
    {// Data.semaphore
        Task[] tasks;
        lock (this.syncObject)
        {
            if (!this.Started)
            {
                return;
            }

            tasks = this.data.Directories.Select(x => x.StopAsync()).ToArray();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        this.Started = false;
    }

    internal void Terminate()
    {
        lock (this.syncObject)
        {
            this.ClearGoshujin();
        }
    }

    internal async Task WaitForCompletionAsync()
    {
        Task[] tasks;
        lock (this.syncObject)
        {
            tasks = this.data.Directories.Select(x => x.WaitForCompletionAsync()).ToArray();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    internal byte[] Serialize()
    {
        lock (this.syncObject)
        {
            return TinyhandSerializer.Serialize(this.data);
        }
    }

    private StorageAndFiler? GetStorageFromId(ushort storageId)
    {// lock (this.syncObject)
        if (storageId == 0)
        {
            return null;
        }

        this.storageObjects.StorageIdChain.TryGetValue(storageId, out var storageObject);
        return storageObject;
    }

    private ushort GetFreeDirectoryId(CrystalDirectory.GoshujinClass goshujin)
    {// lock(syncObject)
        while (true)
        {
            var id = (ushort)RandomVault.Pseudo.NextUInt32();
            if (id != 0 && !goshujin.DirectoryIdChain.ContainsKey(id))
            {
                return id;
            }
        }
    }

    private StorageAndFiler? GetValidStorage()
    {// lock(syncObject)
        var array = this.storageObjects.StorageIdChain.ToArray();
        if (array == null)
        {
            return null;
        }

        return array.MinBy(a => a.GetUsageRatio());
    }

    private void ClearGoshujin()
    {// lock(syncObject)
        foreach (var x in this.storageObjects)
        {
            x.Dispose();
        }

        this.storageObjects.Clear();
    }

    internal UnitLogger UnitLogger { get; }

    private object syncObject = new();
    private StorageAndFiler.GoshujinClass storageObjects = new();  // lock(syncObject)
    private StorageAndFiler? currentStorage; // lock(syncObject)
    private int storageRotationCount; // lock(syncObject)
}
