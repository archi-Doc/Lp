// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401

namespace CrystalData;

public sealed class BigCrystalObject<TData> : IBigCrystal<TData>
    where TData : BaseData, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{// BigCrystalObject = CrystalObject + Datum + StorageGroup (+ Himo)
    public BigCrystalObject(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.crystal = new CrystalObject<TData>(this.Crystalizer);
        this.CrystalConfiguration = CrystalConfiguration.Default;
        this.BigCrystalConfiguration = BigCrystalConfiguration.Default;
        this.storageGroup = new(crystalizer);
        this.himoGoshujin = new(this); // tempcode
        this.logger = crystalizer.UnitLogger.GetLogger<IBigCrystal<TData>>();
    }

    #region FieldAndProperty

    private SemaphoreLock semaphore = new();
    private ICrystal<TData> crystal;
    private StorageGroup storageGroup;
    private HimoGoshujinClass himoGoshujin;
    private ILogger logger;

    #endregion

    #region ICrystal

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration CrystalConfiguration { get; private set; }

    public BigCrystalConfiguration BigCrystalConfiguration { get; private set; }

    public DatumRegistry DatumRegistry { get; } = new();

    public StorageGroup StorageGroup => this.storageGroup;

    public HimoGoshujinClass Himo => this.himoGoshujin;

    public long MemoryUsage => this.himoGoshujin.MemoryUsage;

    public bool Prepared { get; private set; }

    public TData Object => this.crystal.Object;

    object ICrystal.Object => this.crystal.Object;

    public IStorage Storage => this.crystal.Storage;

    void IBigCrystal.Configure(BigCrystalConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.BigCrystalConfiguration = configuration;
            this.CrystalConfiguration = configuration;

            this.BigCrystalConfiguration.RegisterDatum(this.DatumRegistry);
            this.StorageGroup.Configure(this.BigCrystalConfiguration.DirectoryConfiguration.CombinePath(this.BigCrystalConfiguration.StorageFile));
            this.crystal.Configure(configuration);

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

            // Save & Unload datum and metadata.
            this.Object.Save(unload);

            // Save crystal
            await this.crystal.Save(unload);

            // Save storage group
            await this.StorageGroup.SaveGroup().ConfigureAwait(false);

            this.logger.TryGet()?.Log($"Crystal stop - {this.himoGoshujin.MemoryUsage}");
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

            await this.DeleteAllInternal().ConfigureAwait(false);

            // Clear
            this.CrystalConfiguration = CrystalConfiguration.Default;
            this.BigCrystalConfiguration = BigCrystalConfiguration.Default;

            return CrystalResult.Success;
        }
    }

    void ICrystal.Configure(CrystalConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    void ICrystal.ConfigureFile(FileConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    void ICrystal.ConfigureStorage(StorageConfiguration configuration)
    {
        throw new NotImplementedException();
    }

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

    void ICrystal.Terminate()
    {
    }

    #endregion

    private async Task DeleteAllInternal()
    {
        await this.crystal.Delete().ConfigureAwait(false);
        this.himoGoshujin.Clear();
        await this.StorageGroup.DeleteAllAsync().ConfigureAwait(false);

        this.Object.Initialize(this, null, true);
    }

    private async Task<CrystalResult> PrepareAndLoadInternal(CrystalPrepare prepare)
    {// this.semaphore.Lock()
        CrystalResult result;
        var param = prepare.ToParam<TData>(this.Crystalizer);

        if (prepare.CreateNew)
        {
            await this.StorageGroup.PrepareAndLoad(this.CrystalConfiguration.StorageConfiguration, param).ConfigureAwait(false);

            await this.DeleteAllInternal();

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

        result = await this.crystal.PrepareAndLoad(param).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        this.Object.Initialize(this, null, true);
        this.himoGoshujin.Clear();

        var info = this.StorageGroup.GetInformation();
        foreach (var x in info)
        {
            this.logger.TryGet()?.Log(x);
        }

        this.Prepared = true;
        return result;
    }
}
