// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using CrystalData.Datum;

namespace CrystalData;

public partial class Crystal<TData> : ICrystalDataObsolete, ICrystalInternal
    where TData : BaseData
{
    internal Crystal(UnitCore core, CrystalOptions options, ILogger<Crystal<TData>> logger, UnitLogger unitLogger, IStorageKey storageKey)
        : this(core, options, (ILogger)logger, unitLogger, storageKey)
    {
    }

    protected Crystal(UnitCore core, CrystalOptions options, ILogger logger, UnitLogger unitLogger, IStorageKey storageKey)
    {
        this.logger = logger;
        this.Core = core;
        this.Options = options;
        this.Storage = new(unitLogger, storageKey);
        this.himoGoshujin = new(this);
        this.InitializeRoot();

        this.Datum = new();
        this.Datum.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
        // this.Constructor.Register<FragmentDatum<Identifier>>(x => new FragmentDatumImpl<Identifier>(x));
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

            // this.logger.TryGet()?.Log("Crystal start");

            if (param.FromScratch)
            {
                await this.Storage.TryStart(this.Options, param, null).ConfigureAwait(false);

                await this.DeleteAllAsync();
                this.InitializeRoot();

                this.Started = true;
                return CrystalStartResult.Success;
            }

            // Load CrystalStorage
            result = await this.LoadCrystalStorage(param).ConfigureAwait(false);
            if (result != CrystalStartResult.Success)
            {
                return result;
            }

            var info = this.Storage.GetInformation();
            foreach (var x in info)
            {
                this.logger.TryGet()?.Log(x);
            }

            // Load Crystal
            result = await this.LoadCrystal(param).ConfigureAwait(false);
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

    public async Task StopAsync(CrystalStopParam param)
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
                await this.DeleteAllAsync();

                // Stop storage
                await this.Storage.Terminate().ConfigureAwait(false);
                this.Storage.Clear();

                return;
            }

            // Save & Unload datum and metadaba.
            this.Root.Save(true);

            // Stop storage
            await this.Storage.Save().ConfigureAwait(false);
            await this.Storage.Terminate().ConfigureAwait(false);

            // Save data
            await this.Save(this.Options.CrystalFilePath, this.Options.CrystalBackupPath).ConfigureAwait(false);

            // Save storage
            await this.Storage.Save(this.Options.StorageFilePath, this.Options.StorageBackupPath).ConfigureAwait(false);

            this.Storage.Clear();

            this.logger.TryGet()?.Log($"Crystal stop - {this.himoGoshujin.MemoryUsage}");
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

            // HimoGoshujin
            this.himoGoshujin.Stop();

            await this.Storage.Save().ConfigureAwait(false);
            await this.Storage.Terminate().ConfigureAwait(false);
            this.Storage.Clear();
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    HimoGoshujinClass ICrystalInternal.HimoGoshujin => this.himoGoshujin;

    public UnitCore Core { get; init; }

    public TData Root { get; private set; }

    public DatumRegistry Datum { get; private set; }

    public CrystalOptions Options { get; set; } = CrystalOptions.Default;

    public bool Started { get; private set; }

    public StorageControl Storage { get; }

    public HimoGoshujinClass Himo => this.himoGoshujin;

    public long MemoryUsage => this.himoGoshujin.MemoryUsage;

    internal async Task DeleteAllAsync()
    {
        this.Root.Delete();
        this.himoGoshujin.Clear();

        PathHelper.TryDeleteFile(this.Options.CrystalFilePath);
        PathHelper.TryDeleteFile(this.Options.CrystalBackupPath);
        PathHelper.TryDeleteFile(this.Options.StorageFilePath);
        PathHelper.TryDeleteFile(this.Options.StorageBackupPath);
        await this.Storage.DeleteAllAsync();

        try
        {
            Directory.Delete(this.Options.RootPath);
        }
        catch
        {
        }

        this.InitializeRoot();
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
        await this.IO.StopAsync().ConfigureAwait(false);
    }*/

    private HimoGoshujinClass himoGoshujin;

    [MemberNotNull(nameof(Root))]
    private void InitializeRoot()
    {
        this.Root = TinyhandSerializer.Reconstruct<TData>();
        this.Root.Initialize(this, null, true);
    }

    private async Task<CrystalStartResult> LoadCrystalStorage(CrystalStartParam param)
    {// await this.semaphore.WaitAsync().ConfigureAwait(false)
     // Load
        CrystalStartResult result;
        byte[]? data;
        try
        {
            data = await File.ReadAllBytesAsync(this.Options.StorageFilePath).ConfigureAwait(false);
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

        result = await this.Storage.TryStart(this.Options, param, memory).ConfigureAwait(false);
        if (result == CrystalStartResult.Success || param.ForceStart)
        {
            return CrystalStartResult.Success;
        }

        return result;

LoadBackup:
        try
        {
            data = await File.ReadAllBytesAsync(this.Options.StorageBackupPath).ConfigureAwait(false);
        }
        catch
        {
            if (await param.Query(CrystalStartResult.DirectoryNotFound).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                result = await this.Storage.TryStart(this.Options, param, null).ConfigureAwait(false);
                if (result == CrystalStartResult.Success || param.ForceStart)
                {
                    return CrystalStartResult.Success;
                }

                return result;
            }
            else
            {
                return CrystalStartResult.DirectoryNotFound;
            }
        }

        // Checksum Crystal
        if (!HashHelper.CheckFarmHashAndGetData(data.AsMemory(), out memory))
        {
            if (await param.Query(CrystalStartResult.DirectoryError).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                result = await this.Storage.TryStart(this.Options, param, null).ConfigureAwait(false);
                if (result == CrystalStartResult.Success || param.ForceStart)
                {
                    return CrystalStartResult.Success;
                }

                return result;
            }
            else
            {
                return CrystalStartResult.DirectoryError;
            }
        }

        result = await this.Storage.TryStart(this.Options, param, memory).ConfigureAwait(false);
        if (result == CrystalStartResult.Success || param.ForceStart)
        {
            return CrystalStartResult.Success;
        }

        return result;
    }

    private async Task<CrystalStartResult> LoadCrystal(CrystalStartParam param)
    {// await this.semaphore.WaitAsync().ConfigureAwait(false)
     // Load
        byte[]? data;
        try
        {
            data = await File.ReadAllBytesAsync(this.Options.CrystalFilePath).ConfigureAwait(false);
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

        if (this.DeserializeCrystal(memory))
        {
            return CrystalStartResult.Success;
        }

LoadBackup:
        try
        {
            data = await File.ReadAllBytesAsync(this.Options.CrystalBackupPath).ConfigureAwait(false);
        }
        catch
        {
            if (await param.Query(CrystalStartResult.FileNotFound).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                return CrystalStartResult.Success;
            }
            else
            {
                return CrystalStartResult.FileNotFound;
            }
        }

        // Checksum
        if (!HashHelper.CheckFarmHashAndGetData(data.AsMemory(), out memory))
        {
            if (await param.Query(CrystalStartResult.FileError).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                return CrystalStartResult.Success;
            }
            else
            {
                return CrystalStartResult.FileError;
            }
        }

        // Deserialize
        if (!this.DeserializeCrystal(memory))
        {
            if (await param.Query(CrystalStartResult.FileError).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                return CrystalStartResult.Success;
            }
            else
            {
                return CrystalStartResult.FileError;
            }
        }

        return CrystalStartResult.Success;
    }

    private bool DeserializeCrystal(ReadOnlyMemory<byte> data)
    {
        if (!TinyhandSerializer.TryDeserialize<TData>(data.Span, out var tdata))
        {
            return false;
        }

        tdata.Initialize(this, null, true);
        this.Root = tdata;

        this.himoGoshujin.Clear();

        return true;
    }

    private async Task Save(string path, string? backupPath)
    {
        var byteArray = TinyhandSerializer.Serialize(this.Root);
        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath).ConfigureAwait(false);
    }

    private SemaphoreSlim semaphore = new(1, 1);
    private ILogger logger;
}
