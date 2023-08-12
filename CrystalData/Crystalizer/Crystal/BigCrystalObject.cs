﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData;

public sealed class BigCrystalObject<TData> : IBigCrystalInternal<TData>
    where TData : BaseData, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{// BigCrystalObject = CrystalObject + Datum + StorageGroup (+ Himo)
    public BigCrystalObject(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.BigCrystalConfiguration = BigCrystalConfiguration.Default;

        this.crystal = new CrystalObject<TData>(this.Crystalizer);
        this.storageGroup = new(crystalizer, typeof(TData));
    }

    #region FieldAndProperty

    private ICrystalInternal<TData> crystal;
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

    public CrystalState State { get; private set; }

    public Type DataType => typeof(TData);

    public TData Data
    {
        get
        {
            if (this.State == CrystalState.Prepared)
            {
                return this.crystal.Data;
            }

            using (this.semaphore.Lock())
            {
                if (this.State == CrystalState.Initial)
                {// Initial
                    this.PrepareAndLoadInternal(false).Wait();
                }

                return this.crystal.Data;
            }
        }
    }

    object ICrystal.Data => this.Data;

    public IStorage Storage => this.crystal.Storage;

    void IBigCrystal.Configure(BigCrystalConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.crystal.Configure(configuration);
            this.BigCrystalConfiguration = configuration;
            this.BigCrystalConfiguration.RegisterDatum(this.DatumRegistry);
            this.StorageGroup.Configure(this.BigCrystalConfiguration);

            this.State = CrystalState.Initial;
        }
    }

    void ICrystal.Configure(CrystalConfiguration configuration)
        => this.crystal.Configure(configuration);

    void ICrystal.ConfigureFile(FileConfiguration configuration)
        => this.crystal.ConfigureFile(configuration);

    void ICrystal.ConfigureStorage(StorageConfiguration configuration)
        => this.crystal.ConfigureStorage(configuration);

    async Task<CrystalResult> ICrystal.PrepareAndLoad(bool useQuery)
    {
        using (this.semaphore.Lock())
        {
            if (this.State == CrystalState.Prepared)
            {// Prepared
                return CrystalResult.Success;
            }
            else if (this.State == CrystalState.Deleted)
            {// Deleted
                return CrystalResult.Deleted;
            }

            return await this.PrepareAndLoadInternal(useQuery).ConfigureAwait(false);
        }
    }

    async Task<CrystalResult> ICrystal.Save(bool unload)
    {
        using (this.semaphore.Lock())
        {
            if (this.State == CrystalState.Initial)
            {// Initial
                return CrystalResult.NotPrepared;
            }
            else if (this.State == CrystalState.Deleted)
            {// Deleted
                return CrystalResult.Deleted;
            }

            // Save & Unload datum and metadata.
            this.Data.Save(unload);

            // Save crystal
            await this.crystal.Save(unload);

            // Save storages
            await this.StorageGroup.SaveStorage().ConfigureAwait(false);

            // Save storage group
            await this.StorageGroup.SaveGroup().ConfigureAwait(false);
        }

        return CrystalResult.Success;
    }

    async Task<CrystalResult> ICrystal.Delete()
    {
        using (this.semaphore.Lock())
        {
            if (this.State == CrystalState.Initial)
            {// Initial
                await this.PrepareAndLoadInternal(false).ConfigureAwait(false);
            }
            else if (this.State == CrystalState.Deleted)
            {// Deleted
                return CrystalResult.Success;
            }

            var param = PrepareParam.NoQuery<TData>(this.Crystalizer);

            this.Data.Unload();
            await this.crystal.Delete().ConfigureAwait(false);

            await this.StorageGroup.PrepareAndLoad(this.CrystalConfiguration.StorageConfiguration, param).ConfigureAwait(false);
            await this.StorageGroup.DeleteAllAsync().ConfigureAwait(false);

            this.Data.Initialize(this, null, true);

            this.State = CrystalState.Deleted;
            return CrystalResult.Success;
        }
    }

    void ICrystal.Terminate()
    {
    }

    Task? ICrystalInternal.TryPeriodicSave(DateTime utc)
    {
        if (this.CrystalConfiguration.SavePolicy != SavePolicy.Periodic)
        {
            return null;
        }

        var elapsed = utc - this.lastSaveTime;
        if (elapsed < this.CrystalConfiguration.SaveInterval)
        {
            return null;
        }

        this.lastSaveTime = utc;
        return ((ICrystal)this).Save(false);
    }

    Waypoint ICrystalInternal.Waypoint
        => this.crystal.Waypoint;

    async Task ICrystalInternal.TestJournal()
    {
        await this.crystal.TestJournal().ConfigureAwait(false);
        await this.StorageGroup.TestJournal().ConfigureAwait(false);
    }

    #endregion

    #region ITinyhandJournal

    bool ITinyhandJournal.TryGetJournalWriter(JournalType recordType, out TinyhandWriter writer)
    {
        if (this.Crystalizer.Journal is not null)
        {
            this.Crystalizer.Journal.GetWriter(recordType, out writer);
            return true;
        }
        else
        {
            writer = default;
            return false;
        }
    }

    ulong ITinyhandJournal.AddJournal(in TinyhandWriter writer)
    {
        if (this.Crystalizer.Journal is not null)
        {
            return this.Crystalizer.Journal.Add(writer);
        }
        else
        {
            return 0;
        }
    }

    bool ITinyhandJournal.TryAddToSaveQueue()
    {
        if (this.CrystalConfiguration.SavePolicy == SavePolicy.OnChanged)
        {
            this.Crystalizer.AddToSaveQueue(this);
            return true;
        }
        else
        {
            return false;
        }
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

    private async Task<CrystalResult> PrepareAndLoadInternal(bool useQuery)
    {// this.semaphore.Lock()
        CrystalResult result;
        var param = PrepareParam.New<TData>(this.Crystalizer, useQuery);

        result = await this.StorageGroup.PrepareAndLoad(this.CrystalConfiguration.StorageConfiguration, param).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        this.crystal.ConfigureStorage(EmptyStorageConfiguration.Default); // Avoid duplication with the storage configuration of StorageGroup.
        result = await this.crystal.PrepareAndLoad(useQuery).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        this.crystal.Data.Initialize(this, null, true);

        this.State = CrystalState.Prepared;
        return result;
    }
}
