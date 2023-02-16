// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ZenItz.Results;

namespace ZenItz;

public sealed class Storage
{
    public const int DirectoryRotationThreshold = 1024 * 1024 * 100; // 100 MB

    internal Storage()
    {
        this.data = TinyhandSerializer.Reconstruct<StorageData>();
    }

    public ZenDirectoryInformation[] GetDirectoryInformation()
    {
        lock (this.syncObject)
        {
            return this.data.Directories.Select(a => a.GetInformation()).ToArray();
        }
    }

    public AddDictionaryResult AddDirectory(string path, uint id = 0, long capacity = ZenOptions.DefaultDirectoryCapacity)
    {
        if (capacity < 0)
        {
            capacity = ZenOptions.DefaultDirectoryCapacity;
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

            var directory = new ZenDirectory(id, path);
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
        string[] directories;
        lock (this.syncObject)
        {
            directories = this.data.Directories.Select(x => x.RootedPath).ToArray();
        }

        foreach (var x in directories)
        {
            PathHelper.TryDeleteDirectory(x);
        }
    }

    public ZenOptions Options { get; private set; } = ZenOptions.Default;

    public bool Started { get; private set; }

    internal void Save(ref ulong file, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared, int id)
    {
        ZenDirectory? directory;
        lock (this.syncObject)
        {
            if (this.data.Directories.DirectoryIdChain.Count == 0)
            {// No directory available.
                return;
            }
            else if (!ZenHelper.IsValidFile(file) || !this.data.Directories.DirectoryIdChain.TryGetValue(ZenHelper.ToDirectoryId(file), out directory))
            {// Get valid directory.
                if (this.directoryRotationCount >= DirectoryRotationThreshold ||
                    this.currentDirectory == null)
                {
                    this.currentDirectory = this.GetValidDirectory();
                    this.directoryRotationCount = memoryToBeShared.Memory.Length;
                    if (this.currentDirectory == null)
                    {
                        return;
                    }
                }
                else
                {
                    this.directoryRotationCount += memoryToBeShared.Memory.Length;
                }

                directory = this.currentDirectory;
            }

            this.AddMemoryStat(id, memoryToBeShared.Memory.Length);
        }

        directory.Save(ref file, memoryToBeShared);
    }

    internal async Task<ZenMemoryOwnerResult> Load(ulong file)
    {
        if (!ZenHelper.IsValidFile(file))
        {// Invalid file.
            return new(ZenResult.NoData);
        }

        ZenDirectory? directory;
        lock (this.syncObject)
        {
            if (!this.data.Directories.DirectoryIdChain.TryGetValue(ZenHelper.ToDirectoryId(file), out directory))
            {// No directory
                return new(ZenResult.NoDirectory);
            }
        }

        return await directory.Load(file);
    }

    internal void Delete(ulong file)
    {
        if (!ZenHelper.IsValidFile(file))
        {// Invalid file.
            return;
        }

        ZenDirectory? directory;
        lock (this.syncObject)
        {
            if (!this.data.Directories.DirectoryIdChain.TryGetValue(ZenHelper.ToDirectoryId(file), out directory))
            {// No directory
                return;
            }
        }

        directory.Delete(file);
    }

    /*internal void Restart()
    {
        lock (this.syncObject)
        {
            if (this.Started)
            {
                return;
            }

            foreach (var x in this.directoryGoshujin)
            {
                x.PrepareAndCheck(this);
                x.Start();
            }
        }

        this.Started = true;
    }*/

    internal async Task<ZenStartResult> TryStart(ZenOptions options, ZenStartParam param, ReadOnlyMemory<byte>? data)
    {// Zen.semaphore
        if (this.Started)
        {
            return ZenStartResult.Success;
        }

        this.Options = options;

        StorageData? storageData = null;
        if (data != null)
        {
            try
            {
                storageData = TinyhandSerializer.Deserialize<StorageData>(data.Value);
            }
            catch
            {
                if (!await param.Query(ZenStartResult.ZenFileError))
                {
                    return ZenStartResult.ZenFileError;
                }
            }
        }

        storageData ??= TinyhandSerializer.Reconstruct<StorageData>();
        List<string>? errorDirectories = null;
        foreach (var x in storageData.Directories)
        {
            if (!x.PrepareAndCheck(this))
            {
                errorDirectories ??= new();
                errorDirectories.Add(x.DirectoryPath);
            }
        }

        if (errorDirectories != null &&
            !await param.Query(ZenStartResult.ZenDirectoryError, errorDirectories.ToArray()))
        {
            return ZenStartResult.ZenFileError;
        }

        if (storageData.Directories.DirectoryIdChain.Count == 0)
        {
            try
            {
                var defaultDirectory = new ZenDirectory(this.GetFreeDirectoryId(storageData.Directories), PathHelper.GetRootedDirectory(this.Options.RootPath, this.Options.DefaultZenDirectory));
                defaultDirectory.PrepareAndCheck(this);
                storageData.Directories.Add(defaultDirectory);
            }
            catch
            {
            }
        }

        foreach (var x in storageData.Directories)
        {
            x.Start();
        }

        if (storageData.Directories.DirectoryIdChain.Count == 0)
        {
            return ZenStartResult.NoDirectoryAvailable;
        }

        lock (this.syncObject)
        {
            this.ClearGoshujin();
            this.data = storageData;
            this.currentDirectory = null;
            this.Started = true;
        }

        return ZenStartResult.Success;
    }

    internal async Task StopAsync()
    {// Zen.semaphore
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

    private uint GetFreeDirectoryId(ZenDirectory.GoshujinClass goshujin)
    {// lock(syncObject)
        while (true)
        {
            var id = LP.Random.Pseudo.NextUInt32();
            if (id != 0 && !goshujin.DirectoryIdChain.ContainsKey(id))
            {
                return id;
            }
        }
    }

    private ZenDirectory? GetValidDirectory()
    {// lock(syncObject)
        var array = this.data.Directories.ListChain.ToArray();
        if (array == null)
        {
            return null;
        }

        foreach (var x in array)
        {
            x.CalculateUsageRatio();
        }

        return array.MinBy(a => a.UsageRatio);
    }

    private void ClearGoshujin()
    {// lock(syncObject)
        foreach (var x in this.data.Directories)
        {
            x.Dispose();
        }

        this.data.Directories.Clear();
    }

    private void AddMemoryStat(int id, int size)
    {
        if (size != 0)
        {
            if (!this.data.MemoryStats.TryGetValue(id, out var memoryStat))
            {
                memoryStat = TinyhandSerializer.Reconstruct<MemoryStat>();
                this.data.MemoryStats.Add(id, memoryStat);
            }

            memoryStat.Add(size);
        }
    }

    private object syncObject = new();
    private StorageData data; // lock(syncObject)
    private ZenDirectory? currentDirectory; // lock(syncObject)
    private int directoryRotationCount; // lock(syncObject)
}
