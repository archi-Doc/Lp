﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class BigCrystalObject<TData> : CrystalObject<TData>, IBigCrystal<TData>, ICrystal
    where TData : BaseData, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public BigCrystalObject(Crystalizer crystalizer)
        : base(crystalizer)
    {
        this.BigCrystalConfiguration = BigCrystalConfiguration.Default; // crystalizer.GetBigCrystalConfiguration(typeof(TData));
        this.storageGroup = new(crystalizer);
        this.himoGoshujin = new(this);
        this.logger = crystalizer.UnitLogger.GetLogger<IBigCrystal<TData>>();
        this.crystalFileConfiguration = EmptyFileConfiguration.Default;
    }

    #region FieldAndProperty

    public BigCrystalConfiguration BigCrystalConfiguration { get; protected set; }

    public DatumRegistry DatumRegistry { get; } = new();

    public StorageGroup StorageGroup => this.storageGroup;

    public HimoGoshujinClass Himo => this.himoGoshujin;

    public long MemoryUsage => this.himoGoshujin.MemoryUsage;

    private StorageGroup storageGroup;
    private HimoGoshujinClass himoGoshujin;
    private ILogger logger;
    private PathConfiguration crystalFileConfiguration;
    private IFiler? crystalFiler;

    #endregion

    #region ICrystal

    void IBigCrystal.Configure(BigCrystalConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.BigCrystalConfiguration = configuration;
            this.CrystalConfiguration = configuration;

            this.BigCrystalConfiguration.RegisterDatum(this.DatumRegistry);
            this.StorageGroup.Configure(this.BigCrystalConfiguration.DirectoryConfiguration.CombinePath(this.BigCrystalConfiguration.StorageFile));
            this.crystalFileConfiguration = this.BigCrystalConfiguration.DirectoryConfiguration.CombinePath(this.BigCrystalConfiguration.CrystalFile);

            this.storage = null;
            this.Prepared = false;
        }
    }

    async Task<CrystalResult> ICrystal.Save(bool unload)
    {
        using (this.semaphore.Lock())
        {
            if (!this.Prepared)
            {
                await this.PrepareAndLoadInternal(CrystalPrepare.ContinueAll).ConfigureAwait(false);
            }

            // Save storages
            await this.StorageGroup.SaveStorage().ConfigureAwait(false);

            if (this.obj is not null)
            {
                // Save & Unload datum and metadata.
                this.obj.Save(unload);

                if (this.crystalFiler is not null)
                {// Save crystal
                    await PathHelper.SaveData(this.Crystalizer, this.obj, this.crystalFiler, 0).ConfigureAwait(false);
                }
            }

            // Save storage group
            await this.StorageGroup.SaveGroup().ConfigureAwait(false);

            this.logger.TryGet()?.Log($"Crystal stop - {this.himoGoshujin.MemoryUsage}");
        }

        return CrystalResult.Success;
    }

    /*public async Task Abort()
    {
        using (this.semaphore.Lock())
        {
            if (!this.Prepared)
            {
                await this.PrepareAndLoadInternal(CrystalPrepare.ContinueAll).ConfigureAwait(false);
            }

            await this.StorageGroup.SaveStorage().ConfigureAwait(false);
            this.StorageGroup.Clear();
        }
    }*/

    async Task<CrystalResult> ICrystal.Delete()
    {
        using (this.semaphore.Lock())
        {
            if (!this.Prepared)
            {
                await this.PrepareAndLoadInternal(CrystalPrepare.ContinueAll).ConfigureAwait(false);
            }

            await this.DeleteAllInternal().ConfigureAwait(false);

            // Clear
            this.CrystalConfiguration = CrystalConfiguration.Default;

            return CrystalResult.Success;
        }
    }

    internal async Task DeleteAllInternal()
    {
        this.obj?.Delete();
        this.himoGoshujin.Clear();

        this.crystalFiler?.DeleteAndForget();
        this.crystalFiler = null;

        await this.StorageGroup.DeleteAllAsync();

        this.ReconstructObject(true);
    }

    #endregion

    protected override async Task<CrystalResult> PrepareAndLoadInternal(CrystalPrepare prepare)
    {// this.semaphore.Lock()
        CrystalResult result;
        var param = prepare.ToParam<TData>(this.Crystalizer);

        if (prepare.CreateNew)
        {
            await this.StorageGroup.PrepareAndLoad(this.CrystalConfiguration.StorageConfiguration, param).ConfigureAwait(false);

            await this.DeleteAllInternal();
            this.ReconstructObject(true);

            this.Prepared = true;
            return CrystalResult.Success;
        }

        if (this.Prepared)
        {
            return CrystalResult.Success;
        }

        result = await this.StorageGroup.PrepareAndLoad(this.CrystalConfiguration.StorageConfiguration, param).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        var info = this.StorageGroup.GetInformation();
        foreach (var x in info)
        {
            this.logger.TryGet()?.Log(x);
        }

        // Crystal filer
        if (this.crystalFiler == null)
        {
            this.crystalFiler = this.Crystalizer.ResolveFiler(this.crystalFileConfiguration);
            result = await this.crystalFiler.PrepareAndCheck(param, this.crystalFileConfiguration).ConfigureAwait(false);
            if (result.IsFailure())
            {
                return result;
            }
        }

        // Load Crystal
        result = await this.LoadCrystal(prepare).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        this.Prepared = true;
        return result;
    }

    protected override void ReconstructObject(bool createNew)
    {// this.semaphore.Lock()
        if (this.obj == null || createNew)
        {
            this.obj = TinyhandSerializer.Reconstruct<TData>();
            this.obj.Initialize(this, null, true);
            this.himoGoshujin.Clear();
        }
    }

    private async Task<CrystalResult> LoadCrystal(CrystalPrepare param)
    {// await this.semaphore.WaitAsync().ConfigureAwait(false)
        var (dataResult, _) = await PathHelper.LoadData(this.crystalFiler!).ConfigureAwait(false);
        if (dataResult.IsFailure)
        {
            if (await param.Query(this.crystalFileConfiguration, dataResult.Result).ConfigureAwait(false) == AbortOrContinue.Abort)
            {
                return dataResult.Result;
            }

            this.ReconstructObject(false);
        }

        if (!this.DeserializeCrystal(dataResult.Data.Memory))
        {
            if (await param.Query(this.crystalFileConfiguration, CrystalResult.DeserializeError).ConfigureAwait(false) == AbortOrContinue.Abort)
            {
                return CrystalResult.DeserializeError;
            }

            this.ReconstructObject(false);
        }

        return CrystalResult.Success;
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
}
