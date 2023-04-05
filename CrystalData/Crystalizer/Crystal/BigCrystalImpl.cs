// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class BigCrystalImpl<TData> : CrystalImpl<TData>, IBigCrystal<TData>, ICrystal
    where TData : BaseData, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public BigCrystalImpl(Crystalizer crystalizer)
        : base(crystalizer)
    {
        this.BigCrystalConfiguration = crystalizer.GetBigCrystalConfiguration(typeof(TData));
        this.BigCrystalConfiguration.RegisterDatum(this.DatumRegistry);
        this.storageGroup = new(crystalizer);
        this.himoGoshujin = new(this);
        this.logger = crystalizer.UnitLogger.GetLogger<IBigCrystal<TData>>();

        this.storageFileConfiguration = this.CrystalConfiguration.FilerConfiguration with { File = this.BigCrystalOptions.StorageFilePath, };
        this.crystalFileConfiguration = this.CrystalConfiguration.FilerConfiguration with { File = this.BigCrystalOptions.CrystalFilePath, };

        this.InitializeRoot();
    }

    #region FieldAndProperty

    public BigCrystalConfiguration BigCrystalConfiguration { get; }

    public BigCrystalOptions BigCrystalOptions => this.BigCrystalConfiguration.BigCrystalOptions;

    public DatumRegistry DatumRegistry { get; } = new();

    public StorageGroup StorageGroup => this.storageGroup;

    public HimoGoshujinClass Himo => this.himoGoshujin;

    public long MemoryUsage => this.himoGoshujin.MemoryUsage;

    private StorageGroup storageGroup;
    private HimoGoshujinClass himoGoshujin;
    private ILogger logger;
    private FilerConfiguration storageFileConfiguration;
    private IFiler? storageFiler;
    private FilerConfiguration crystalFileConfiguration;
    private IFiler? crystalFiler;

    #endregion

    public async Task<CrystalStartResult> PrepareAndLoad(CrystalStartParam? param)
    {
        param ??= CrystalStartParam.Default;
        using (this.semaphore.Lock())
        {
            this.storageFiler ??= this.Crystalizer.ResolveFiler(this.storageFileConfiguration);
            this.crystalFiler ??= this.Crystalizer.ResolveFiler(this.crystalFileConfiguration);

            if (param.FromScratch)
            {
                await this.StorageGroup.PrepareAndCheck(this.BigCrystalOptions, param, null).ConfigureAwait(false);

                await this.DeleteAllAsync();
                this.InitializeRoot();

                return CrystalStartResult.Success;
            }

            // Load CrystalStorage
            var result = await this.LoadCrystalStorage(param).ConfigureAwait(false);
            if (result != CrystalStartResult.Success)
            {
                return result;
            }

            var info = this.StorageGroup.GetInformation();
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

            return result;
        }
    }

    public async Task StopAsync(CrystalStopParam param)
    {
        using (this.semaphore.Lock())
        {
            if (param.RemoveAll)
            {
                await this.DeleteAllAsync();

                // Stop storage
                this.StorageGroup.Clear();

                return;
            }

            // Save & Unload datum and metadaba.
            this.obj?.Save(true);

            // Stop storage
            await this.StorageGroup.SaveStorage().ConfigureAwait(false);

            // Save data
            await this.Save(this.BigCrystalOptions.CrystalFilePath, this.BigCrystalOptions.CrystalBackupPath).ConfigureAwait(false);

            // Save storage
            await this.StorageGroup.SaveGroup(this.BigCrystalOptions.StorageFilePath, this.BigCrystalOptions.StorageBackupPath).ConfigureAwait(false);

            this.StorageGroup.Clear();

            this.logger.TryGet()?.Log($"Crystal stop - {this.himoGoshujin.MemoryUsage}");
        }
    }

    public async Task Abort()
    {
        using (this.semaphore.Lock())
        {
            await this.StorageGroup.SaveStorage().ConfigureAwait(false);
            this.StorageGroup.Clear();
        }
    }

    internal async Task DeleteAllAsync()
    {
        this.obj?.Delete();
        this.himoGoshujin.Clear();

        PathHelper.TryDeleteFile(this.BigCrystalOptions.CrystalFilePath);
        PathHelper.TryDeleteFile(this.BigCrystalOptions.CrystalBackupPath);
        PathHelper.TryDeleteFile(this.BigCrystalOptions.StorageFilePath);
        PathHelper.TryDeleteFile(this.BigCrystalOptions.StorageBackupPath);
        await this.StorageGroup.DeleteAllAsync();

        try
        {
            Directory.Delete(this.BigCrystalOptions.RootPath);
        }
        catch
        {
        }

        this.InitializeRoot();
    }

    private void InitializeRoot()
    {
        this.obj = TinyhandSerializer.Reconstruct<TData>();
        this.obj.Initialize(this, null, true);
    }

    private async Task<CrystalStartResult> LoadCrystalStorage(CrystalStartParam param)
    {// await this.semaphore.WaitAsync().ConfigureAwait(false)
        CrystalStartResult result;

        var filerResult = await this.storageFiler!.ReadAsync(0, -1).ConfigureAwait(false);
        if (filerResult.IsFailure ||
            !HashHelper.CheckFarmHashAndGetData(filerResult.Data.Memory, out var memory))
        {
            if (await param.Query(CrystalStartResult.DirectoryNotFound).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                result = await this.StorageGroup.PrepareAndCheck(this.BigCrystalOptions, param, null).ConfigureAwait(false);
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

        result = await this.StorageGroup.PrepareAndCheck(this.BigCrystalOptions, param, memory).ConfigureAwait(false);
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
            data = await File.ReadAllBytesAsync(this.BigCrystalOptions.CrystalFilePath).ConfigureAwait(false);
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
            data = await File.ReadAllBytesAsync(this.BigCrystalOptions.CrystalBackupPath).ConfigureAwait(false);
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
        this.obj = tdata;

        this.himoGoshujin.Clear();

        return true;
    }

    private async Task Save(string path, string? backupPath)
    {
        var byteArray = TinyhandSerializer.Serialize(this.obj);
        await HashHelper.GetFarmHashAndSaveAsync(byteArray, path, backupPath).ConfigureAwait(false);
    }
}
