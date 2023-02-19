// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;

namespace CrystalData;

public partial class Crystal<TData> : ICrystalInternal
    where TData : BaseData
{
    internal Crystal(UnitCore core, CrystalOptions options, ILogger<Crystal<TData>> logger)
    {
        this.logger = logger;
        this.Core = core;
        this.Options = options;
        this.Storage = new();
        this.himoGoshujin = new(this);
        this.Data = TinyhandSerializer.Reconstruct<TData>();

        this.Constructor = new();
        this.Constructor.Register<BlockDatum>(x => new BlockDatumImpl(x));
        this.Constructor.Register<FragmentDatum<Identifier>>(x => new FragmentDatumImpl<Identifier>(x));
    }

    public async Task<CrystalStartResult> StartAsync(CrystalStartParam param)
    {
        await this.semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var result = CrystalStartResult.Success;
            if (this.Started)
            {
                return CrystalStartResult.Success;
            }

            this.logger.TryGet()?.Log("Zen start");

            if (param.FromScratch)
            {
                await this.Storage.TryStart(this.Options, param, null);

                this.DeleteAll();

                this.Started = true;
                return CrystalStartResult.Success;
            }

            // Load CrystalDirectory
            result = await this.LoadZenDirectory(param);
            if (result != CrystalStartResult.Success)
            {
                return result;
            }

            // Load Zen
            result = await this.LoadZen(param);
            if (result != CrystalStartResult.Success)
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

                // Stop IO(CrystalDirectory)
                await this.Storage.StopAsync();
                this.Storage.Terminate();

                return;
            }

            // Save & Unload flakes
            this.Data.Save(true);

            // Stop IO(CrystalDirectory)
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

    HimoGoshujinClass ICrystalInternal.HimoGoshujin => this.himoGoshujin;

    public UnitCore Core { get; init; }

    public TData Data { get; private set; }

    public DataConstructor Constructor { get; private set; }

    public CrystalOptions Options { get; set; } = CrystalOptions.Default;

    public bool Started { get; private set; }

    public Storage Storage { get; }

    public long MemoryUsage => this.himoGoshujin.MemoryUsage;

    internal void DeleteAll()
    {
        this.Data.Delete();
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

        // Stop IO(CrystalDirectory)
        await this.IO.StopAsync();
    }*/

    private HimoGoshujinClass himoGoshujin;

    private async Task<CrystalStartResult> LoadZenDirectory(CrystalStartParam param)
    {// await this.semaphore.WaitAsync()
        // Load
        CrystalStartResult result;
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
        if (result == CrystalStartResult.Success || param.ForceStart)
        {
            return CrystalStartResult.Success;
        }

        return result;

LoadBackup:
        try
        {
            data = await File.ReadAllBytesAsync(this.Options.ZenDirectoryBackupPath);
        }
        catch
        {
            if (await param.Query(CrystalStartResult.ZenDirectoryNotFound))
            {
                result = await this.Storage.TryStart(this.Options, param, null);
                if (result == CrystalStartResult.Success || param.ForceStart)
                {
                    return CrystalStartResult.Success;
                }

                return result;
            }
            else
            {
                return CrystalStartResult.ZenDirectoryNotFound;
            }
        }

        // Checksum Zen
        if (!HashHelper.CheckFarmHashAndGetData(data.AsMemory(), out memory))
        {
            if (await param.Query(CrystalStartResult.ZenDirectoryError))
            {
                result = await this.Storage.TryStart(this.Options, param, null);
                if (result == CrystalStartResult.Success || param.ForceStart)
                {
                    return CrystalStartResult.Success;
                }

                return result;
            }
            else
            {
                return CrystalStartResult.ZenDirectoryError;
            }
        }

        result = await this.Storage.TryStart(this.Options, param, memory);
        if (result == CrystalStartResult.Success || param.ForceStart)
        {
            return CrystalStartResult.Success;
        }

        return result;
    }

    private async Task<CrystalStartResult> LoadZen(CrystalStartParam param)
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
            return CrystalStartResult.Success;
        }

LoadBackup:
        try
        {
            data = await File.ReadAllBytesAsync(this.Options.ZenBackupPath);
        }
        catch
        {
            if (await param.Query(CrystalStartResult.ZenFileNotFound))
            {
                return CrystalStartResult.Success;
            }
            else
            {
                return CrystalStartResult.ZenFileNotFound;
            }
        }

        // Checksum Zen
        if (!HashHelper.CheckFarmHashAndGetData(data.AsMemory(), out memory))
        {
            if (await param.Query(CrystalStartResult.ZenFileError))
            {
                return CrystalStartResult.Success;
            }
            else
            {
                return CrystalStartResult.ZenFileError;
            }
        }

        // Deserialize
        if (!this.DeserializeZen(memory))
        {
            if (await param.Query(CrystalStartResult.ZenFileError))
            {
                return CrystalStartResult.Success;
            }
            else
            {
                return CrystalStartResult.ZenFileError;
            }
        }

        return CrystalStartResult.Success;
    }

    private bool DeserializeZen(ReadOnlyMemory<byte> data)
    {
        if (!TinyhandSerializer.TryDeserialize<BaseData>(data.Span, out var baseData))
        {
            return false;
        }

        baseData.Initialize(this, null, true);

        this.himoGoshujin.Clear();

        return true;
    }

    private async Task SerializeZen(string path, string? backupPath)
    {
        var byteArray = TinyhandSerializer.Serialize(this.Data);
        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath);
    }

    private SemaphoreSlim semaphore = new(1, 1);
    private ILogger logger;
}
