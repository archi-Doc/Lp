// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

namespace CrystalData;

public sealed class BigCrystalObject<TData> : IBigCrystalInternal<TData>
    where TData : BaseData, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{// BigCrystalObject = CrystalObject + Datum + StorageGroup (+ Himo)
    public BigCrystalObject(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.BigCrystalConfiguration = BigCrystalConfiguration.Default;

        this.crystal = new CrystalObject<TData>(this.Crystalizer);
        this.storageGroup = new(crystalizer);
    }

    #region FieldAndProperty

    private ICrystal<TData> crystal;
    private SemaphoreLock semaphore = new();
    private StorageGroup storageGroup;
    private DateTime lastSaveTime;

    #endregion

    #region ICrystal

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration CrystalConfiguration => this.crystal.CrystalConfiguration;

    public BigCrystalConfiguration BigCrystalConfiguration { get; private set; }

    public DatumRegistry DatumRegistry { get; } = new();

    public StorageGroup StorageGroup => this.storageGroup;

    public bool Prepared { get; private set; }

    public TData Object => this.crystal.Object;

    object ICrystal.Object => this.crystal.Object;

    public IStorage Storage => this.crystal.Storage;

    void IBigCrystal.Configure(BigCrystalConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.crystal.Configure(configuration);
            this.BigCrystalConfiguration = configuration;
            this.BigCrystalConfiguration.RegisterDatum(this.DatumRegistry);
            this.StorageGroup.Configure(this.BigCrystalConfiguration.FileConfiguration.AppendPath(this.BigCrystalConfiguration.StorageGroupExtension));

            this.Prepared = false;
        }
    }

    void ICrystal.Configure(CrystalConfiguration configuration)
        => this.crystal.Configure(configuration);

    void ICrystal.ConfigureFile(FileConfiguration configuration)
        => this.crystal.ConfigureFile(configuration);

    void ICrystal.ConfigureStorage(StorageConfiguration configuration)
        => this.crystal.ConfigureStorage(configuration);

    async Task<CrystalResult> ICrystal.PrepareAndLoad(CrystalPrepare param)
    {
        using (this.semaphore.Lock())
        {
            if (this.Prepared)
            {// Prepared
                return CrystalResult.Success;
            }

            return await this.PrepareAndLoadInternal(param).ConfigureAwait(false);
        }
    }

    async Task<CrystalResult> ICrystal.Save(bool unload)
    {
        using (this.semaphore.Lock())
        {
            if (!this.Prepared)
            {
                return CrystalResult.NotPrepared;
            }

            // Save storages
            await this.StorageGroup.SaveStorage().ConfigureAwait(false);

            // Save & Unload datum and metadata.
            this.Object.Save(unload);

            // Save crystal
            await this.crystal.Save(unload);

            // Save storage group
            await this.StorageGroup.SaveGroup().ConfigureAwait(false);
        }

        return CrystalResult.Success;
    }

    async Task<CrystalResult> ICrystal.Delete()
    {
        using (this.semaphore.Lock())
        {
            if (!this.Prepared)
            {
                await this.PrepareAndLoadInternal(CrystalPrepare.ContinueAll).ConfigureAwait(false);
            }

            var param = PrepareParam.ContinueAll<TData>(this.Crystalizer);

            this.Object.Unload();
            await this.crystal.Delete().ConfigureAwait(false);

            await this.StorageGroup.PrepareAndLoad(this.CrystalConfiguration.StorageConfiguration, param).ConfigureAwait(false);
            await this.StorageGroup.DeleteAllAsync().ConfigureAwait(false);

            this.Object.Initialize(this, null, true);

            return CrystalResult.Success;
        }
    }

    void ICrystal.Terminate()
    {
    }

    bool ICrystalInternal.CheckPeriodicSave(DateTime utc)
    {
        if (this.CrystalConfiguration.SavePolicy != SavePolicy.Periodic)
        {
            return false;
        }

        var elapsed = utc - this.lastSaveTime;
        if (elapsed < this.CrystalConfiguration.SaveInterval)
        {
            return false;
        }

        this.lastSaveTime = utc;
        return true;
    }

    #endregion

    public void Status()
    {
        var logger = this.Crystalizer.UnitLogger.GetLogger<IBigCrystal<TData>>();
        var info = this.StorageGroup.GetInformation();
        foreach (var x in info)
        {
            logger.TryGet()?.Log(x);
        }
    }

    private async Task<CrystalResult> PrepareAndLoadInternal(CrystalPrepare prepare)
    {// this.semaphore.Lock()
        CrystalResult result;
        var param = prepare.ToParam<TData>(this.Crystalizer);

        result = await this.StorageGroup.PrepareAndLoad(this.CrystalConfiguration.StorageConfiguration, param).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        result = await this.crystal.PrepareAndLoad(param).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        this.Object.Initialize(this, null, true);

        this.Prepared = true;
        return result;
    }
}
