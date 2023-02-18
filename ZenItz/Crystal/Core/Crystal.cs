// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;

namespace ZenItz.Crystal.Core;

public partial class Crystal<TData> : IZenInternal
    where TData : BaseData
{
    internal Crystal(UnitCore core, CrystalOptions options, ILogger<Crystal<TData>> logger)
    {
        this.logger = logger;
        Core = core;
        Options = options;
        Storage = new();
        himoGoshujin = new(this);
        Data = TinyhandSerializer.Reconstruct<TData>();

        Constructor = new();
        Constructor.Register<BlockDatum>(x => new BlockDatumImpl(x));
        Constructor.Register<FragmentData<Identifier>>(x => new FragmentDataImpl<Identifier>(x));
    }

    public async Task<ZenStartResult> StartAsync(ZenStartParam param)
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var result = ZenStartResult.Success;
            if (Started)
            {
                return ZenStartResult.Success;
            }

            logger.TryGet()?.Log("Zen start");

            if (param.FromScratch)
            {
                await Storage.TryStart(Options, param, null);

                DeleteAll();

                Started = true;
                return ZenStartResult.Success;
            }

            // Load ZenDirectory
            result = await LoadZenDirectory(param);
            if (result != ZenStartResult.Success)
            {
                return result;
            }

            // Load Zen
            result = await LoadZen(param);
            if (result != ZenStartResult.Success)
            {
                return result;
            }

            // HimoGoshujin
            himoGoshujin.Start();

            Started = true;
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task StopAsync(ZenStopParam param)
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!Started)
            {
                return;
            }

            Started = false;

            // HimoGoshujin
            himoGoshujin.Stop();

            if (param.RemoveAll)
            {
                DeleteAll();

                // Stop IO(ZenDirectory)
                await Storage.StopAsync();
                Storage.Terminate();

                return;
            }

            // Save & Unload flakes
            Data.Save(true);

            // Stop IO(ZenDirectory)
            await Storage.StopAsync();

            // Save Zen
            await SerializeZen(Options.ZenFilePath, Options.ZenBackupPath);

            // Save directory information
            var byteArray = Storage.Serialize();
            await HashHelper.GetFarmHashAndSaveAsync(byteArray, Options.ZenDirectoryFilePath, Options.ZenDirectoryBackupPath);

            Storage.Terminate();

            logger.TryGet()?.Log($"Zen stop - {himoGoshujin.MemoryUsage}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task Abort()
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!Started)
            {
                return;
            }

            Started = false;

            await Storage.StopAsync();
            Storage.Terminate();
        }
        finally
        {
            semaphore.Release();
        }
    }

    HimoGoshujinClass IZenInternal.HimoGoshujin => himoGoshujin;

    public UnitCore Core { get; init; }

    public BaseData Data { get; private set; }

    public DataConstructor Constructor { get; private set; }

    public CrystalOptions Options { get; set; } = CrystalOptions.Default;

    public bool Started { get; private set; }

    public Storage Storage { get; }

    public long MemoryUsage => himoGoshujin.MemoryUsage;

    internal void DeleteAll()
    {
        Data.Delete();
        himoGoshujin.Clear();

        PathHelper.TryDeleteFile(Options.ZenFilePath);
        PathHelper.TryDeleteFile(Options.ZenBackupPath);
        PathHelper.TryDeleteFile(Options.ZenDirectoryFilePath);
        PathHelper.TryDeleteFile(Options.ZenDirectoryBackupPath);
        Storage.DeleteAll();

        try
        {
            Directory.Delete(Options.RootPath);
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
            data = await File.ReadAllBytesAsync(Options.ZenDirectoryFilePath);
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

        result = await Storage.TryStart(Options, param, memory);
        if (result == ZenStartResult.Success || param.ForceStart)
        {
            return ZenStartResult.Success;
        }

        return result;

LoadBackup:
        try
        {
            data = await File.ReadAllBytesAsync(Options.ZenDirectoryBackupPath);
        }
        catch
        {
            if (await param.Query(ZenStartResult.ZenDirectoryNotFound))
            {
                result = await Storage.TryStart(Options, param, null);
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
                result = await Storage.TryStart(Options, param, null);
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

        result = await Storage.TryStart(Options, param, memory);
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
            data = await File.ReadAllBytesAsync(Options.ZenFilePath);
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

        if (DeserializeZen(memory))
        {
            return ZenStartResult.Success;
        }

LoadBackup:
        try
        {
            data = await File.ReadAllBytesAsync(Options.ZenBackupPath);
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
        if (!DeserializeZen(memory))
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
        if (!TinyhandSerializer.TryDeserialize<BaseData>(data.Span, out var baseData))
        {
            return false;
        }

        baseData.DeserializePostProcess(this);

        himoGoshujin.Clear();

        return true;
    }

    private async Task SerializeZen(string path, string? backupPath)
    {
        var byteArray = TinyhandSerializer.Serialize(Data);
        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath);
    }

    private SemaphoreSlim semaphore = new(1, 1);
    private ILogger logger;
}
