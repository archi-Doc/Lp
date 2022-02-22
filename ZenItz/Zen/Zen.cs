// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public class Zen
{
    public const int MaxFlakeSize = 1024 * 1024 * 4; // 4MB
    public const int MaxFragmentSize = 1024 * 4; // 4KB
    public const int MaxFragmentCount = 1000;
    public const long DefaultMemorySizeLimit = 1024 * 1024 * 100; // 100MB
    public const long DefaultDirectoryCapacity = 1024L * 1024 * 1024 * 10; // 10GB

    public const string DefaultZenDirectory = "Zen";
    public const string DefaultZenFile = "Zen.main";
    public const string DefaultZenBackup = "Zen.back";
    public const string DefaultZenDirectoryFile = "ZenDirectory.main";
    public const string DefaultZenDirectoryBackup = "ZenDirectory.back";
    public const string DefaultDirectoryFile = "Directory.main";
    public const string DefaultDirectoryBackup = "Directory.back";

    public delegate void ObjectToMemoryOwnerDelegate(object? obj, out ByteArrayPool.MemoryOwner dataToBeMoved);

    public delegate object? MemoryOwnerToObjectDelegate(ByteArrayPool.MemoryOwner memoryOwner);

    public static void DefaultObjectToMemoryOwner(object? obj, out ByteArrayPool.MemoryOwner dataToBeMoved)
    {
        dataToBeMoved = ByteArrayPool.MemoryOwner.Empty;
    }

    public static object? DefaultMemoryOwnerToObject(ByteArrayPool.MemoryOwner memoryOwner)
    {
        return null;
    }

    public Zen()
    {
        this.FlakeFragmentPool = new ByteArrayPool(MaxFlakeSize, (int)(DefaultMemorySizeLimit / MaxFlakeSize));
        this.FlakeFragmentPool.SetMaxPoolBelow(MaxFragmentSize, 0);
        this.IO = new(this.FlakeFragmentPool);
        this.FlakeObjectGoshujin = new(this, this.FlakeFragmentPool);
        this.FragmentObjectGoshujin = new(this, this.FlakeFragmentPool);
    }

    public async Task<ZenStartResult> TryStartZen(ZenStartParam param)
    {
        var result = ZenStartResult.Success;
        if (this.Started)
        {
            return ZenStartResult.AlreadyStarted;
        }

        // Load ZenDirectory
        result = await this.LoadZenDirectory(param);
        if (result != ZenStartResult.Success)
        {
            return result;
        }

        // Load Zen
        result = await this.LoadZen(param);
        if (result != ZenStartResult.Success)
        {
            return result;
        }

        this.Started = true;
        return result;
    }

    public async Task StopZen(ZenStopParam param)
    {
        if (!this.Started)
        {
            return;
        }

        this.Started = false;

        // Save & Unload flakes
        lock (this.flakeGoshujin)
        {
            foreach (var x in this.flakeGoshujin.IdChain)
            {
                x.Save(true);
            }
        }

        // Stop IO(ZenDirectory)
        await this.IO.StopAsync();

        // Save Zen
        await this.SerializeZen(param.ZenFile, param.ZenBackup);

        // Save directory information
        var byteArray = this.IO.Serialize();
        await HashHelper.GetFarmHashAndSaveAsync(byteArray, param.ZenDirectoryFile, param.ZenDirectoryBackup);
    }

    public void SetDelegate(ObjectToMemoryOwnerDelegate objectToMemoryOwner, MemoryOwnerToObjectDelegate memoryOwnerToObject)
    {
        this.ObjectToMemoryOwner = objectToMemoryOwner;
        this.MemoryOwnerToObject = memoryOwnerToObject;
    }

    public Flake CreateOrGet(Identifier id)
    {
        Flake? flake;
        lock (this.flakeGoshujin)
        {
            if (!this.flakeGoshujin.IdChain.TryGetValue(id, out flake))
            {
                flake = new Flake(this, id);
                this.flakeGoshujin.Add(flake);
            }
        }

        return flake;
    }

    public Flake? TryGet(Identifier id)
    {
        Flake? flake;
        lock (this.flakeGoshujin)
        {
            this.flakeGoshujin.IdChain.TryGetValue(id, out flake);
            return flake;
        }
    }

    public bool Remove(Identifier id)
    {
        lock (this.flakeGoshujin)
        {
            if (this.flakeGoshujin.IdChain.TryGetValue(id, out var flake))
            {
                return flake.RemoveInternal();
            }
        }

        return false;
    }

    public bool Started { get; private set; }

    public ZenIO IO { get; }

    public ByteArrayPool FlakeFragmentPool { get; }

    public ObjectToMemoryOwnerDelegate ObjectToMemoryOwner { get; private set; } = DefaultObjectToMemoryOwner;

    public MemoryOwnerToObjectDelegate MemoryOwnerToObject { get; private set; } = DefaultMemoryOwnerToObject;

    public void Restart()
    {
        if (this.Started)
        {
            return;
        }

        this.IO.Restart();

        this.Started = true;
    }

    internal async Task Pause()
    {
        if (!this.Started)
        {
            return;
        }

        this.Started = false;

        // Save & Unload flakes
        lock (this.flakeGoshujin)
        {
            foreach (var x in this.flakeGoshujin.IdChain)
            {
                x.Save(true);
            }
        }

        // Stop IO(ZenDirectory)
        await this.IO.StopAsync();
    }

    internal FlakeObjectGoshujin FlakeObjectGoshujin;
    internal FlakeObjectGoshujin FragmentObjectGoshujin;
    private Flake.GoshujinClass flakeGoshujin = new();

    private async Task<ZenStartResult> LoadZenDirectory(ZenStartParam param)
    {
        // Load
        ZenStartResult result;
        byte[]? data;
        try
        {
            data = await File.ReadAllBytesAsync(param.ZenDirectoryFile);
        }
        catch
        {
            goto LoadBackup;
        }

        // Checksum
        if (!HashHelper.CheckFarmHashAndGetData(data.AsMemory(), out var memory))
        {
            goto LoadBackup;
        }

        result = await this.IO.TryStart(param, memory);
        if (result == ZenStartResult.Success || param.ForceStart)
        {
            return ZenStartResult.Success;
        }

        return result;

LoadBackup:
        try
        {
            data = await File.ReadAllBytesAsync(param.ZenDirectoryBackup);
        }
        catch
        {
            if (await param.Query(ZenStartResult.ZenDirectoryNotFound))
            {
                result = await this.IO.TryStart(param, null);
                if (result == ZenStartResult.Success || param.ForceStart)
                {
                    return ZenStartResult.Success;
                }

                return result;
            }
            else
            {
                return ZenStartResult.ZenDirectoryNotFound;
            }
        }

        // Checksum Zen
        if (!HashHelper.CheckFarmHashAndGetData(data.AsMemory(), out memory))
        {
            if (await param.Query(ZenStartResult.ZenDirectoryError))
            {
                result = await this.IO.TryStart(param, null);
                if (result == ZenStartResult.Success || param.ForceStart)
                {
                    return ZenStartResult.Success;
                }

                return result;
            }
            else
            {
                return ZenStartResult.ZenDirectoryError;
            }
        }

        result = await this.IO.TryStart(param, memory);
        if (result == ZenStartResult.Success || param.ForceStart)
        {
            return ZenStartResult.Success;
        }

        return result;
    }

    private async Task<ZenStartResult> LoadZen(ZenStartParam param)
    {
        // Load
        byte[]? data;
        try
        {
            data = await File.ReadAllBytesAsync(param.ZenFile);
        }
        catch
        {
            goto LoadBackup;
        }

        // Checksum
        if (!HashHelper.CheckFarmHashAndGetData(data.AsMemory(), out var memory))
        {
            goto LoadBackup;
        }

        if (this.DeserializeZen(memory))
        {
            return ZenStartResult.Success;
        }

LoadBackup:
        try
        {
            data = await File.ReadAllBytesAsync(param.ZenBackup);
        }
        catch
        {
            if (await param.Query(ZenStartResult.ZenFileNotFound))
            {
                return ZenStartResult.Success;
            }
            else
            {
                return ZenStartResult.ZenFileNotFound;
            }
        }

        // Checksum Zen
        if (!HashHelper.CheckFarmHashAndGetData(data.AsMemory(), out memory))
        {
            if (await param.Query(ZenStartResult.ZenFileError))
            {
                return ZenStartResult.Success;
            }
            else
            {
                return ZenStartResult.ZenFileError;
            }
        }

        // Deserialize
        if (!this.DeserializeZen(memory))
        {
            if (await param.Query(ZenStartResult.ZenFileError))
            {
                return ZenStartResult.Success;
            }
            else
            {
                return ZenStartResult.ZenFileError;
            }
        }

        return ZenStartResult.Success;
    }

    private bool DeserializeZen(ReadOnlyMemory<byte> data)
    {
        if (!TinyhandSerializer.TryDeserialize<Flake.GoshujinClass>(data, out var g))
        {
            return false;
        }

        foreach (var x in g)
        {
            x.Zen = this;
        }

        lock (this.flakeGoshujin)
        {
            this.FlakeObjectGoshujin.Goshujin.Clear();
            this.FragmentObjectGoshujin.Goshujin.Clear();
            this.flakeGoshujin = g;
        }

        return true;
    }

    private async Task SerializeZen(string path, string? backupPath)
    {
        byte[]? byteArray;
        lock (this.flakeGoshujin)
        {
            byteArray = TinyhandSerializer.Serialize(this.flakeGoshujin);
        }

        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath);
    }
}
