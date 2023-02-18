﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public class Zen : Zen<Identifier>
{
    public Zen(UnitCore core, ZenOptions options, ILogger<Zen<Identifier>> logger)
        : base(core, options, logger)
    {
    }
}

public partial class Zen<TIdentifier> : IZenInternal
    where TIdentifier : IEquatable<TIdentifier>, IComparable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
    internal Zen(UnitCore core, ZenOptions options, ILogger<Zen<TIdentifier>> logger)
    {
        this.logger = logger;
        this.Core = core;
        this.Options = options;
        this.Storage = new();
        this.himoGoshujin = new(this);
        this.Root = new(this);

        this.Constructor = new();
        this.Constructor.Register<BlockData>(x => new BlockDataImpl(x));
        this.Constructor.Register<FragmentData<TIdentifier>>(x => new FragmentDataImpl<TIdentifier>(x));
    }

    public async Task<ZenStartResult> StartAsync(ZenStartParam param)
    {
        await this.semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var result = ZenStartResult.Success;
            if (this.Started)
            {
                return ZenStartResult.Success;
            }

            this.logger.TryGet()?.Log("Zen start");

            if (param.FromScratch)
            {
                await this.Storage.TryStart(this.Options, param, null);

                this.DeleteAll();

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

            // HimoGoshujin
            this.himoGoshujin.Start();

            this.Started = true;
            return result;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public async Task StopAsync(ZenStopParam param)
    {
        await this.semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!this.Started)
            {
                return;
            }

            this.Started = false;

            // HimoGoshujin
            this.himoGoshujin.Stop();

            if (param.RemoveAll)
            {
                this.DeleteAll();

                // Stop IO(ZenDirectory)
                await this.Storage.StopAsync();
                this.Storage.Terminate();

                return;
            }

            // Save & Unload flakes
            this.Root.Save(true);

            // Stop IO(ZenDirectory)
            await this.Storage.StopAsync();

            // Save Zen
            await this.SerializeZen(this.Options.ZenFilePath, this.Options.ZenBackupPath);

            // Save directory information
            var byteArray = this.Storage.Serialize();
            await HashHelper.GetFarmHashAndSaveAsync(byteArray, this.Options.ZenDirectoryFilePath, this.Options.ZenDirectoryBackupPath);

            this.Storage.Terminate();

            this.logger.TryGet()?.Log($"Zen stop - {this.himoGoshujin.MemoryUsage}");
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public async Task Abort()
    {
        await this.semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!this.Started)
            {
                return;
            }

            this.Started = false;

            await this.Storage.StopAsync();
            this.Storage.Terminate();
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    HimoGoshujinClass IZenInternal.HimoGoshujin => this.himoGoshujin;

    public UnitCore Core { get; init; }

    public DataConstructor Constructor { get; private set; }

    public ZenOptions Options { get; set; } = ZenOptions.Default;

    public bool Started { get; private set; }

    public RootFlake Root { get; private set; }

    public Storage Storage { get; }

    public long MemoryUsage => this.himoGoshujin.MemoryUsage;

    internal void DeleteAll()
    {
        this.Root.DeleteInternal();
        this.himoGoshujin.Clear();

        PathHelper.TryDeleteFile(this.Options.ZenFilePath);
        PathHelper.TryDeleteFile(this.Options.ZenBackupPath);
        PathHelper.TryDeleteFile(this.Options.ZenDirectoryFilePath);
        PathHelper.TryDeleteFile(this.Options.ZenDirectoryBackupPath);
        this.Storage.DeleteAll();

        try
        {
            Directory.Delete(this.Options.RootPath);
        }
        catch
        {
        }
    }

    /*internal void Restart()
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
    }*/

    private HimoGoshujinClass himoGoshujin;

    private async Task<ZenStartResult> LoadZenDirectory(ZenStartParam param)
    {// await this.semaphore.WaitAsync()
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

        result = await this.Storage.TryStart(this.Options, param, memory);
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
                result = await this.Storage.TryStart(this.Options, param, null);
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
                result = await this.Storage.TryStart(this.Options, param, null);
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

        result = await this.Storage.TryStart(this.Options, param, memory);
        if (result == ZenStartResult.Success || param.ForceStart)
        {
            return ZenStartResult.Success;
        }

        return result;
    }

    private async Task<ZenStartResult> LoadZen(ZenStartParam param)
    {// await this.semaphore.WaitAsync()
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

        this.himoGoshujin.Clear();

        return true;
    }

    private async Task SerializeZen(string path, string? backupPath)
    {
        var byteArray = TinyhandSerializer.SerializeObject((Flake)this.Root);
        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath);
    }

    private SemaphoreSlim semaphore = new(1, 1);
    private ILogger logger;
}