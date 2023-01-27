// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LPEssentials;

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public class Zen : Zen<Identifier>
{
}

public partial class Zen<TIdentifier>
    where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
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

    public Zen(ZenOptions? options = null)
    {
        this.Options = options ?? ZenOptions.Default;
        this.IO = new();
        this.FlakeObjectGoshujin = new(this);
        this.FragmentObjectGoshujin = new(this);
        this.Root = new(this, null, default!);
    }

    public async Task<ZenStartResult> Start(ZenStartParam param)
    {
        var result = ZenStartResult.Success;
        if (this.Started)
        {
            return ZenStartResult.Success;
        }

        if (param.FromScratch)
        {
            this.RemoveAll();
            await this.IO.TryStart(this.Options, param, null);
            this.Started = true;
            return ZenStartResult.Success;
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

    public async Task Stop(ZenStopParam param)
    {
        if (!this.Started)
        {
            return;
        }

        this.Started = false;

        if (param.RemoveAll)
        {
            // Stop IO(ZenDirectory)
            await this.IO.StopAsync();

            this.RemoveAll();
            return;
        }

        // Save & Unload flakes
        this.Root.Save(true);

        // Stop IO(ZenDirectory)
        await this.IO.StopAsync();

        // Save Zen
        await this.SerializeZen(this.Options.ZenFilePath, this.Options.ZenBackupPath);

        // Save directory information
        var byteArray = this.IO.Serialize();
        await HashHelper.GetFarmHashAndSaveAsync(byteArray, this.Options.ZenDirectoryFilePath, this.Options.ZenDirectoryBackupPath);
    }

    public async Task Abort()
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

    public ZenOptions Options { get; set; }

    public bool Started { get; private set; }

    public Flake Root { get; private set; }

    public ZenIO IO { get; }

    public ObjectToMemoryOwnerDelegate ObjectToMemoryOwner { get; private set; } = DefaultObjectToMemoryOwner;

    public MemoryOwnerToObjectDelegate MemoryOwnerToObject { get; private set; } = DefaultMemoryOwnerToObject;

    internal void RemoveAll()
    {
        this.Root.RemoveInternal();

        lock (this.FlakeObjectGoshujin.Goshujin)
        {
            this.FlakeObjectGoshujin.Goshujin.Clear();
        }

        lock (this.FragmentObjectGoshujin.Goshujin)
        {
            this.FragmentObjectGoshujin.Goshujin.Clear();
        }

        PathHelper.TryDeleteFile(this.Options.ZenFilePath);
        PathHelper.TryDeleteFile(this.Options.ZenBackupPath);
        PathHelper.TryDeleteFile(this.Options.ZenDirectoryFilePath);
        PathHelper.TryDeleteFile(this.Options.ZenDirectoryBackupPath);
        this.IO.RemoveAll();

        try
        {
            Directory.Delete(this.Options.RootPath);
        }
        catch
        {
        }
    }

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
        this.Root.Save();

        // Stop IO(ZenDirectory)
        await this.IO.StopAsync();
    }

    internal HimoGoshujinClass FlakeObjectGoshujin;
    internal HimoGoshujinClass FragmentObjectGoshujin;

    private async Task<ZenStartResult> LoadZenDirectory(ZenStartParam param)
    {
        // Load
        ZenStartResult result;
        byte[]? data;
        try
        {
            data = await File.ReadAllBytesAsync(this.Options.ZenDirectoryFilePath);
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

        result = await this.IO.TryStart(this.Options, param, memory);
        if (result == ZenStartResult.Success || param.ForceStart)
        {
            return ZenStartResult.Success;
        }

        return result;

LoadBackup:
        try
        {
            data = await File.ReadAllBytesAsync(this.Options.ZenDirectoryBackupPath);
        }
        catch
        {
            if (await param.Query(ZenStartResult.ZenDirectoryNotFound))
            {
                result = await this.IO.TryStart(this.Options, param, null);
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
                result = await this.IO.TryStart(this.Options, param, null);
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

        result = await this.IO.TryStart(this.Options, param, memory);
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
            data = await File.ReadAllBytesAsync(this.Options.ZenFilePath);
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
            data = await File.ReadAllBytesAsync(this.Options.ZenBackupPath);
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
        if (!TinyhandSerializer.TryDeserialize<Flake>(data.Span, out var flake))
        {
            return false;
        }

        flake.DeserializePostProcess(this);

        this.FlakeObjectGoshujin.Goshujin.Clear();
        this.FragmentObjectGoshujin.Goshujin.Clear();

        return true;
    }

    private async Task SerializeZen(string path, string? backupPath)
    {
        byte[]? byteArray;
        lock (this.Root.syncObject)
        {
            byteArray = TinyhandSerializer.Serialize(this.Root);
        }

        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath);
    }
}
