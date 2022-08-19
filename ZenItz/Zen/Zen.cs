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
    public const string DefaultDirectoryFile = "Snowflake.main";
    public const string DefaultDirectoryBackup = "Snowflake.back";

    public delegate bool ObjectToMemoryOwnerDelegate(object? obj, out ByteArrayPool.MemoryOwner dataToBeMoved);

    public delegate object? MemoryOwnerToObjectDelegate(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner);

    public static bool DefaultObjectToMemoryOwner(object? obj, out ByteArrayPool.MemoryOwner dataToBeMoved)
    {
        dataToBeMoved = ByteArrayPool.MemoryOwner.Empty;
        return false;
    }

    public static object? DefaultMemoryOwnerToObject(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
        return null;
    }

    public Zen(UnitLogger unitLogger, ZenIO io)
    {
        Zen.UnitLogger = unitLogger;
        this.IO = io;
        this.FlakeObjectGoshujin = new(this);
        this.FragmentObjectGoshujin = new(this);
    }

    public ZenStartResult StartZenForTest()
    {
        if (this.Started)
        {
            return ZenStartResult.AlreadyStarted;
        }

        this.Started = true;
        return ZenStartResult.Success;
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
            foreach (var x in this.flakeGoshujin)
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

    public async Task AbortZen()
    {
        if (!this.Started)
        {
            return;
        }

        this.Started = false;

        await this.IO.StopAsync();
    }

    public void SetDelegate(ObjectToMemoryOwnerDelegate objectToMemoryOwner, MemoryOwnerToObjectDelegate memoryOwnerToObject)
    {
        this.ObjectToMemoryOwner = objectToMemoryOwner;
        this.MemoryOwnerToObject = memoryOwnerToObject;
    }

    public Flake? TryCreateOrGet(Identifier id)
    {
        if (!this.Started)
        {
            return null;
        }

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
        if (!this.Started)
        {
            return null;
        }

        Flake? flake;
        lock (this.flakeGoshujin)
        {
            this.flakeGoshujin.IdChain.TryGetValue(id, out flake);
            return flake;
        }
    }

    public bool Remove(Identifier id)
    {
        if (!this.Started)
        {
            return false;
        }

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

    public ObjectToMemoryOwnerDelegate ObjectToMemoryOwner { get; private set; } = DefaultObjectToMemoryOwner;

    public MemoryOwnerToObjectDelegate MemoryOwnerToObject { get; private set; } = DefaultMemoryOwnerToObject;

    internal void Restart()
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
            foreach (var x in this.flakeGoshujin)
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

    internal static UnitLogger UnitLogger { get; private set; } = default!;
}
