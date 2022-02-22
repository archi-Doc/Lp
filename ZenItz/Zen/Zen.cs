// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public class Zen
{
    public const int MaxFlakeSize = 1024 * 1024 * 4; // 4MB
    public const int MaxFragmentSize = 1024 * 4; // 4KB
    public const int MaxFragmentCount = 1000;
    public const long DefaultMemorySizeLimit = 1024 * 1024 * 100; // 100MB
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
        this.IO = new();
        this.FlakePool = new ByteArrayPool(MaxFlakeSize, (int)(DefaultMemorySizeLimit / MaxFlakeSize));
        this.FragmentPool = new ByteArrayPool(MaxFragmentSize, 0);
        this.FlakeObjectGoshujin = new(this, this.FlakePool);
        this.FragmentObjectGoshujin = new(this, this.FragmentPool);
    }

    public async Task<ZenStartResult> TryStartZen(ZenStartParam param)
    {
        var result = ZenStartResult.Success;
        if (this.ZenStarted)
        {
            return ZenStartResult.AlreadyStarted;
        }

        // Load
        byte[]? zenData;
        try
        {
            zenData = await File.ReadAllBytesAsync(param.ZenFile);
        }
        catch
        {
            if (await param.Query(ZenStartResult.ZenFileNotFound))
            {
                result = await this.IO.TryStart(param, null);
                if (result == ZenStartResult.Success || param.ForceStart)
                {
                    this.ZenStarted = true;
                }

                return result;
            }
            else
            {
                return ZenStartResult.ZenFileNotFound;
            }
        }

        // Checksum
        if (!HashHelper.CheckFarmHashAndGetData(zenData.AsMemory(), out var memory))
        {
            if (await param.Query(ZenStartResult.ZenFileError))
            {
                result = await this.IO.TryStart(param, null);
                if (result == ZenStartResult.Success || param.ForceStart)
                {
                    this.ZenStarted = true;
                    return result;
                }
            }
            else
            {
                return ZenStartResult.ZenFileError;
            }
        }

        result = await this.IO.TryStart(param, memory);
        if (result != ZenStartResult.Success && !param.ForceStart)
        {
            return result;
        }

        // Load
        this.LoadZen(memory);

        this.ZenStarted = true;
        return result;
    }

    public async Task StopZen(ZenStopParam param)
    {
        if (!this.ZenStarted)
        {
            return;
        }

        this.ZenStarted = false;

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
        await this.SaveZen(param.ZenFile, param.ZenBackup);

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
            else
            {
                flake.CreateInternal(this.flakeGoshujin);
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

    public bool TryRemove(Identifier id)
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

    public ZenIO IO { get; }

    public ByteArrayPool FlakePool { get; }

    public ByteArrayPool FragmentPool { get; }

    public ObjectToMemoryOwnerDelegate ObjectToMemoryOwner { get; private set; } = DefaultObjectToMemoryOwner;

    public MemoryOwnerToObjectDelegate MemoryOwnerToObject { get; private set; } = DefaultMemoryOwnerToObject;

    internal volatile bool ZenStarted;
    internal FlakeObjectGoshujin FlakeObjectGoshujin;
    internal FlakeObjectGoshujin FragmentObjectGoshujin;
    private Flake.GoshujinClass flakeGoshujin = new();

    private bool LoadZen(ReadOnlyMemory<byte> data)
    {
        if (!TinyhandSerializer.TryDeserialize<Flake.GoshujinClass>(data, out var g))
        {
            return false;
        }

        lock (this.flakeGoshujin)
        {
            this.FlakeObjectGoshujin.Goshujin.Clear();
            this.FragmentObjectGoshujin.Goshujin.Clear();
            this.flakeGoshujin = g;
        }

        return true;
    }

    private async Task SaveZen(string path, string? backupPath)
    {
        byte[]? byteArray;
        lock (this.flakeGoshujin)
        {
            byteArray = TinyhandSerializer.Serialize(this.flakeGoshujin);
        }

        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath);
    }
}
