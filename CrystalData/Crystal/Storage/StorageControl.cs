// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;
using CrystalData.Results;
using CrystalData.Storage;

namespace CrystalData;

public sealed class StorageControl
{
    public const int DirectoryRotationThreshold = (int)StorageHelper.Megabytes * 100; // 100 MB

    internal StorageControl(UnitLogger unitLogger, IStorageKey key)
    {
        this.UnitLogger = unitLogger;
        this.Key = key;
    }

    public string[] GetInformation()
    {
        lock (this.syncObject)
        {
            return this.storageAndFilers.Select(x => x.ToString()).ToArray();
        }
    }

    public bool CheckStorageId(ushort id) => this.storageAndFilers.StorageIdChain.ContainsKey(id);

    public bool DeleteStorage(ushort id)
    {// Dev stage
        lock (this.syncObject)
        {
            if (!this.storageAndFilers.StorageIdChain.TryGetValue(id, out var storageAndFiler))
            {// Not found
                return false;
            }

            // Move files to other storage...

            // Deletion
            // storageAndFiler.Filer?.DeleteAllAsync().Wait();
            storageAndFiler.Terminate().Wait();
            storageAndFiler.Goshujin = null;

            return true;
        }
    }

    public (AddStorageResult Result, ushort Id) AddStorage_SimpleLocal(string path, long capacity)
    {
        if (capacity < 0)
        {
            capacity = CrystalOptions.DefaultDirectoryCapacity;
        }

        path = path.TrimEnd('\\');
        if (File.Exists(path))
        {
            return (AddStorageResult.WriteError, 0);
        }

        var relative = Path.GetRelativePath(this.Options.RootPath, path);
        if (!relative.StartsWith("..\\"))
        {
            path = relative;
        }

        var result = LocalFiler.Check(default!, path);
        if (result != AddStorageResult.Success)
        {
            return (result, 0);
        }

        ushort id;
        lock (this.syncObject)
        {
            id = this.GetFreeStorageId();

            var storage = new SimpleStorage();
            storage.StorageCapacity = capacity;

            var filer = new LocalFiler();

            var storageAndFiler = TinyhandSerializer.Reconstruct<StorageAndFiler>();
            storageAndFiler.StorageId = id;
            storageAndFiler.Storage = storage;
            storageAndFiler.Filer = filer;

            if (storageAndFiler.PrepareAndCheck(this, true).Result != CrystalResult.Success)
            {
                storageAndFiler.Terminate().Wait();
                return (AddStorageResult.DuplicatePath, 0);
            }

            storageAndFiler.Goshujin = this.storageAndFilers;
        }

        return (AddStorageResult.Success, id);
    }

    public (AddStorageResult Result, ushort Id) AddStorage_SimpleS3(string bucket, string path, long capacity)
    {
        if (capacity < 0)
        {
            capacity = CrystalOptions.DefaultDirectoryCapacity;
        }

        path = path.TrimEnd('\\');
        var result = S3Filer.Check(this, bucket, path);
        if (result != AddStorageResult.Success)
        {
            return (result, 0);
        }

        ushort id;
        lock (this.syncObject)
        {
            id = this.GetFreeStorageId();

            var storage = new SimpleStorage();
            storage.StorageCapacity = capacity;

            var filer = new S3Filer(bucket, path);

            var storageAndFiler = TinyhandSerializer.Reconstruct<StorageAndFiler>();
            storageAndFiler.StorageId = id;
            storageAndFiler.Storage = storage;
            storageAndFiler.Filer = filer;

            if (storageAndFiler.PrepareAndCheck(this, true).Result != CrystalResult.Success)
            {
                storageAndFiler.Terminate().Wait();
                return (AddStorageResult.DuplicatePath, 0);
            }

            storageAndFiler.Goshujin = this.storageAndFilers;
        }

        return (AddStorageResult.Success, id);
    }

    public async Task DeleteAllAsync()
    {
        var list = new List<Task>();
        lock (this.syncObject)
        {
            foreach (var x in this.storageAndFilers)
            {// tempcode
                /*var task = x.Filer?.DeleteAllAsync();
                if (task != null)
                {
                    list.Add(task);
                }*/
            }
        }

        await Task.WhenAll(list).ConfigureAwait(false);
    }

    public CrystalOptions Options { get; private set; } = CrystalOptions.Default;

    public IStorageKey Key { get; }

    public bool Started { get; private set; }

    public void Save(ref ushort storageId, ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared, ushort datumId)
    {
        StorageAndFiler? storage;
        lock (this.syncObject)
        {
            if (this.storageAndFilers.StorageIdChain.Count == 0)
            {// No storage available.
                return;
            }
            else if (storageId == 0 || !this.storageAndFilers.StorageIdChain.TryGetValue(storageId, out storage))
            {// Get valid directory.
                if (this.storageRotationCount >= DirectoryRotationThreshold ||
                    this.currentStorageAndFiler == null)
                {
                    this.currentStorageAndFiler = this.GetValidStorage();
                    this.storageRotationCount = memoryToBeShared.Memory.Length;
                    if (this.currentStorageAndFiler == null)
                    {
                        return;
                    }
                }
                else
                {
                    this.storageRotationCount += memoryToBeShared.Memory.Length;
                }

                storage = this.currentStorageAndFiler;
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
                return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NoStorage));
            }
        }

        var task = storageObject.Storage?.GetAsync(ref fileId, TimeSpan.MinValue);
        if (task == null)
        {
            return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NoStorage));
        }

        return task;
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
                if (await param.Query(CrystalStartResult.FileError).ConfigureAwait(false) == AbortOrComplete.Abort)
                {
                    return CrystalStartResult.FileError;
                }
            }
        }

        goshujin ??= TinyhandSerializer.Reconstruct<StorageAndFiler.GoshujinClass>();
        List<StorageAndFiler>? errorList = null;
        foreach (var x in goshujin.StorageIdChain)
        {
            if (await x.PrepareAndCheck(this, false) != CrystalResult.Success)
            {
                errorList ??= new();
                errorList.Add(x);
            }
        }

        if (errorList != null &&
            await param.Query(CrystalStartResult.DirectoryError, errorList.Select(x => x.ToString()).ToArray()).ConfigureAwait(false) == AbortOrComplete.Abort)
        {
            return CrystalStartResult.FileError;
        }

        if (errorList != null)
        {
            foreach (var x in errorList)
            {
                x.Goshujin = null;
            }
        }

        if (goshujin.StorageIdChain.Count == 0)
        {
            try
            {
                var storage = new SimpleStorage();
                storage.StorageCapacity = CrystalOptions.DefaultDirectoryCapacity;
                var filer = new LocalFiler(); // PathHelper.GetRootedDirectory(this.Options.RootPath, this.Options.DefaultCrystalDirectory)
                // var filer = new S3Filer("kiokubako", "lp");

                var storageAndFiler = TinyhandSerializer.Reconstruct<StorageAndFiler>();
                storageAndFiler.StorageId = this.GetFreeStorageId();
                storageAndFiler.Storage = storage;
                storageAndFiler.Filer = filer;
                await storageAndFiler.PrepareAndCheck(this, true).ConfigureAwait(false);

                storageAndFiler.Goshujin = goshujin;
            }
            catch
            {
            }
        }

        foreach (var x in goshujin)
        {
            x.Start();
        }

        if (goshujin.StorageIdChain.Count == 0)
        {
            return CrystalStartResult.NoDirectoryAvailable;
        }

        lock (this.syncObject)
        {
            this.storageAndFilers.Clear();
            this.storageAndFilers = goshujin;
            this.currentStorageAndFiler = null;
            this.Started = true;
        }

        return CrystalStartResult.Success;
    }

    internal async Task Save()
    {// Save
        Task[] tasks;
        lock (this.syncObject)
        {
            if (!this.Started)
            {
                return;
            }

            tasks = this.storageAndFilers.Select(x => x.Save()).ToArray();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    internal async Task Terminate()
    {// Save storage
        Task[] tasks;
        lock (this.syncObject)
        {
            if (!this.Started)
            {
                return;
            }

            tasks = this.storageAndFilers.Select(x => x.Terminate()).ToArray();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        this.Started = false;
    }

    internal async Task Save(string path, string? backupPath)
    {// Save storage information
        byte[] byteArray;
        lock (this.syncObject)
        {
            byteArray = TinyhandSerializer.Serialize(this.storageAndFilers);
        }

        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath).ConfigureAwait(false);
    }

    internal void Clear()
    {
        lock (this.syncObject)
        {
            this.storageAndFilers.Clear();
        }
    }

    private StorageAndFiler? GetStorageFromId(ushort storageId)
    {// lock (this.syncObject)
        if (storageId == 0)
        {
            return null;
        }

        this.storageAndFilers.StorageIdChain.TryGetValue(storageId, out var storageObject);
        return storageObject;
    }

    private ushort GetFreeStorageId()
    {// lock(syncObject)
        while (true)
        {
            var id = (ushort)RandomVault.Pseudo.NextUInt32();
            if (id != 0 && !this.storageAndFilers.StorageIdChain.ContainsKey(id))
            {
                return id;
            }
        }
    }

    private StorageAndFiler? GetValidStorage()
    {// lock(syncObject)
        var array = this.storageAndFilers.StorageIdChain.ToArray();
        if (array == null)
        {
            return null;
        }

        return array.MinBy(a => a.GetUsageRatio());
    }

    internal UnitLogger UnitLogger { get; }

    private object syncObject = new();
    private StorageAndFiler.GoshujinClass storageAndFilers = new();  // lock(syncObject)
    private StorageAndFiler? currentStorageAndFiler; // lock(syncObject)
    private int storageRotationCount; // lock(syncObject)
}
