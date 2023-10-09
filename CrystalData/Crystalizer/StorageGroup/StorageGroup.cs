// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CrystalData.Filer;
using CrystalData.Results;

namespace CrystalData;

public sealed class StorageGroup
{
    public const long DefaultStorageCapacity = 1024L * 1024 * 1024 * 10; // 10GB
    public const int StorageRotationThreshold = (int)StorageHelper.Megabytes * 100; // 100 MB

    internal StorageGroup(Crystalizer crystalizer, Type dataType)
    {
        this.Crystalizer = crystalizer;
        this.dataType = dataType;
        this.crystal = this.Crystalizer.CreateCrystal<StorageObject.GoshujinClass>(null, false);
        this.storageGroupConfiguration = CrystalConfiguration.Default;
    }

    #region PropertyAndField

    public Crystalizer Crystalizer { get; }

    private Type dataType;
    private CrystalConfiguration storageGroupConfiguration;

    private StorageObject.GoshujinClass storages => this.crystal.Data;

    private object syncObject = new();
    private ICrystal<StorageObject.GoshujinClass> crystal; // lock(syncObject)
    private StorageObject? currentStorage; // lock(syncObject)
    private int storageRotationCount; // lock(syncObject)

    #endregion

    public void Configure(BigCrystalConfiguration configuration)
    {
        var fileConfiguration = configuration.FileConfiguration.AppendPath(configuration.StorageGroupExtension);
        var backupConfiguration = configuration.BackupFileConfiguration?.AppendPath(configuration.StorageGroupExtension);
        this.storageGroupConfiguration = new CrystalConfiguration(SavePolicy.Manual, fileConfiguration)
        {
            BackupFileConfiguration = backupConfiguration,
            NumberOfFileHistories = 0,
        };

        this.crystal.Configure(this.storageGroupConfiguration);
    }

    public string[] GetInformation()
    {
        lock (this.syncObject)
        {
            return this.storages.Select(x => x.ToString()).ToArray();
        }
    }

    public bool CheckStorageId(ushort id) => this.storages.StorageIdChain.ContainsKey(id);

    public bool DeleteStorage(ushort id)
    {// Dev stage
        lock (this.syncObject)
        {
            if (!this.storages.StorageIdChain.TryGetValue(id, out var storageAndFiler))
            {// Not found
                return false;
            }

            // Move files to other storage...

            // Deletion
            storageAndFiler.Goshujin = null;

            return true;
        }
    }

    public (AddStorageResult Result, ushort Id) AddStorage_SimpleLocal(string path, long capacity)
    {
        if (capacity < 0)
        {
            capacity = DefaultStorageCapacity;
        }

        path = path.TrimEnd('\\');
        if (File.Exists(path))
        {
            return (AddStorageResult.WriteError, 0);
        }

        var relativePath = Path.GetRelativePath(this.Crystalizer.RootDirectory, path);
        if (!relativePath.StartsWith("..\\"))
        {
            path = relativePath;
        }

        var result = LocalFiler.Check(this.Crystalizer, path);
        if (result != AddStorageResult.Success)
        {
            return (result, 0);
        }

        ushort id;
        lock (this.syncObject)
        {
            id = this.GetFreeStorageId();
            var configuration = new SimpleStorageConfiguration(new LocalDirectoryConfiguration(path));

            var storageObject = new StorageObject(id, configuration);
            storageObject.StorageCapacity = capacity;
            if (storageObject.PrepareAndCheck(this, PrepareParam.New(this.Crystalizer, this.dataType, true), true).Result != CrystalResult.Success)
            {
                return (AddStorageResult.DuplicatePath, 0);
            }

            storageObject.Goshujin = this.storages;
        }

        return (AddStorageResult.Success, id);
    }

    public (AddStorageResult Result, ushort Id) AddStorage_SimpleS3(string bucket, string path, long capacity)
    {
        if (capacity < 0)
        {
            capacity = DefaultStorageCapacity;
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

            var configuration = new SimpleStorageConfiguration(new S3DirectoryConfiguration(bucket, path));

            var storageObject = new StorageObject(id, configuration);
            storageObject.StorageCapacity = capacity;
            if (storageObject.PrepareAndCheck(this, PrepareParam.New(this.Crystalizer, this.dataType, true), true).Result != CrystalResult.Success)
            {
                return (AddStorageResult.DuplicatePath, 0);
            }

            storageObject.Goshujin = this.storages;
        }

        return (AddStorageResult.Success, id);
    }

    public async Task DeleteAllAsync()
    {
        var list = new List<Task>();
        lock (this.syncObject)
        {
            foreach (var x in this.storages)
            {
                if (x.Storage?.DeleteStorageAsync() is { } task)
                {
                    list.Add(task);
                }
            }
        }

        await this.crystal.Delete().ConfigureAwait(false);
        await Task.WhenAll(list).ConfigureAwait(false);
    }

    public void PutAndForget(ref ushort storageId, ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared, ushort datumId)
    {
        StorageObject? storage;
        lock (this.syncObject)
        {
            if (this.storages.StorageIdChain.Count == 0)
            {// No storage available.
                return;
            }
            else if (storageId == 0 || !this.storages.StorageIdChain.TryGetValue(storageId, out storage))
            {// Get valid directory.
                if (this.storageRotationCount >= StorageRotationThreshold ||
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

        storage.Storage?.PutAndForget(ref fileId, memoryToBeShared);
    }

    public Task<CrystalMemoryOwnerResult> GetAsync(ushort storageId, ulong fileId)
    {
        StorageObject? storageObject;
        lock (this.syncObject)
        {
            storageObject = this.GetStorageFromId(storageId);
            if (storageObject == null)
            {
                return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NotFound));
            }
        }

        var task = storageObject.Storage?.GetAsync(ref fileId);
        if (task == null)
        {
            return Task.FromResult(new CrystalMemoryOwnerResult(CrystalResult.NotFound));
        }

        return task;
    }

    public void DeleteAndForget(ref ushort storageId, ref ulong fileId)
    {
        StorageObject? storageObject;
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

        storageObject.Storage?.DeleteAndForget(ref fileId);
    }

    internal async Task<CrystalResult> PrepareAndLoad(StorageConfiguration defaultConfiguration, PrepareParam param)
    {// semaphore
        if (this.crystal.State == CrystalState.Initial)
        {
            _ = await this.crystal.PrepareAndLoad(param.UseQuery).ConfigureAwait(false);
        }

        foreach (var x in this.storages.StorageIdChain)
        {
            var result = await x.PrepareAndCheck(this, param, false).ConfigureAwait(false);
            if (result != CrystalResult.Success)
            {
                /*if (await param.QueryObsolete(this.storageGroupConfiguration, result).ConfigureAwait(false) == AbortOrContinue.Abort)
                {
                    return CrystalResult.FileOperationError;
                }*/
            }
        }

        if (this.storages.StorageIdChain.Count == 0)
        {
            try
            {
                var id = this.GetFreeStorageId();
                var storageObject = new StorageObject(id, defaultConfiguration);
                await storageObject.PrepareAndCheck(this, param, true).ConfigureAwait(false);
                storageObject.Goshujin = this.storages;
            }
            catch
            {
            }
        }

        if (this.storages.StorageIdChain.Count == 0)
        {
            return CrystalResult.NotPrepared;
        }

        lock (this.syncObject)
        {
            this.currentStorage = null;
        }

        return CrystalResult.Success;
    }

    internal async Task SaveStorage()
    {// Save
        Task[] tasks;
        lock (this.syncObject)
        {
            tasks = this.storages.Select(x => x.Save()).ToArray();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    internal async Task SaveGroup(UnloadMode unloadMode)
    {// Save storage information
        await this.crystal.Save(unloadMode).ConfigureAwait(false);
    }

    internal void Clear()
    {
        lock (this.syncObject)
        {
            this.storages.Clear();
        }
    }

    internal async Task<bool> TestJournal()
    {
        IStorage?[] array;
        lock (this.syncObject)
        {
            array = this.storages.Select(x => x.Storage).ToArray();
        }

        var result = true;
        foreach (var x in array)
        {
            if (x is IStorageInternal storageInternal)
            {
                if (await storageInternal.TestJournal().ConfigureAwait(false) == false)
                {
                    result = false;
                }
            }
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StorageObject? GetStorageFromId(ushort storageId)
    {// lock (this.syncObject)
        if (storageId == 0)
        {
            return null;
        }

        this.storages.StorageIdChain.TryGetValue(storageId, out var storageObject);
        return storageObject;
    }

    private ushort GetFreeStorageId()
    {// lock(syncObject)
        while (true)
        {
            var id = (ushort)RandomVault.Pseudo.NextUInt32();
            if (id != 0 && !this.storages.StorageIdChain.ContainsKey(id))
            {
                return id;
            }
        }
    }

    private StorageObject? GetValidStorage()
    {// lock(syncObject)
        var array = this.storages.StorageIdChain.ToArray();
        if (array == null)
        {
            return null;
        }

        return array.MinBy(a => a.GetUsageRatio());
    }
}
