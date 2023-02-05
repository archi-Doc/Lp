// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public class Zen : Zen<Identifier>
{
    public class RootFlake : Flake
    {
    }

    public Zen(ZenOptions options, ILogger<Zen<Identifier>> logger)
        : base(options, logger)
    {
    }
}

public partial class Zen<TIdentifier>
    where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
    internal Zen(ZenOptions options, ILogger<Zen<TIdentifier>> logger)
    {
        this.logger = logger;
        this.Options = options;
        this.IO = new();
        this.HimoGoshujin = new(this);
        this.Root = new(this, null, default!);
    }

    public async Task<ZenStartResult> Start(ZenStartParam param)
    {
        var result = ZenStartResult.Success;
        if (this.Started)
        {
            return ZenStartResult.Success;
        }

        this.logger.TryGet()?.Log("Zen start");

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

        // HimoGoshujin
        this.HimoGoshujin.Start();

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

        // HimoGoshujin
        this.HimoGoshujin.Stop();

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

        this.logger.TryGet()?.Log($"Zen stop - {this.HimoGoshujin.MemoryUsage}");
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

    public ZenOptions Options { get; set; } = ZenOptions.Default;

    public bool Started { get; private set; }

    public Flake Root { get; private set; }

    public ZenIO IO { get; }

    public long MemoryUsage => this.HimoGoshujin.MemoryUsage;

    internal void RemoveAll()
    {
        this.Root.RemoveInternal();
        this.HimoGoshujin.Clear();

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

    internal HimoGoshujinClass HimoGoshujin;

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

        this.HimoGoshujin.Clear();

        return true;
    }

    private async Task SerializeZen(string path, string? backupPath)
    {
        var byteArray = TinyhandSerializer.Serialize(this.Root);
        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath);
    }

    private ILogger logger;
}
