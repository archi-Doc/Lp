// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CrystalData.Filer;
using Tinyhand.IO;

namespace CrystalData;

public sealed class CrystalObject<TData> : ICrystalInternal<TData>, ITinyhandCrystal
    where TData : class, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{// Data + Journal/Waypoint + Filer/FileConfiguration + Storage/StorageConfiguration
    public CrystalObject(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.CrystalConfiguration = CrystalConfiguration.Default;
    }

    #region FieldAndProperty

    private SemaphoreLock semaphore = new();
    private TData? obj;
    private CrystalFiler? crystalFiler;
    private IStorage? storage;
    private Waypoint waypoint;
    private DateTime lastSaveTime;

    #endregion

    #region ICrystal

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration CrystalConfiguration { get; private set; }

    public Type ObjectType => typeof(TData);

    object ICrystal.Object => ((ICrystal<TData>)this).Object!;

    public TData Object
    {
        get
        {
            if (this.obj is { } v)
            {
                return v;
            }

            using (this.semaphore.Lock())
            {
                if (this.State == CrystalState.Initial)
                {// Initial
                    this.PrepareAndLoadInternal(false).Wait();
                }
                else if (this.State == CrystalState.Deleted)
                {// Deleted
                    TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
                    return this.obj;
                }

                if (this.obj != null)
                {
                    return this.obj;
                }

                // Finally, reconstruct
                this.ReconstructObject();
                return this.obj;
            }
        }
    }

    public CrystalState State { get; private set; }

    /*public IFiler Filer
    {
        get
        {
            if (this.rawFiler is { } v)
            {
                return v;
            }

            using (this.semaphore.Lock())
            {
                if (this.rawFiler != null)
                {
                    return this.rawFiler;
                }

                this.ResolveAndPrepareFiler();
                return this.rawFiler;
            }
        }
    }*/

    public IStorage Storage
    {
        get
        {
            if (this.storage is { } v)
            {
                return v;
            }

            using (this.semaphore.Lock())
            {
                if (this.storage != null)
                {
                    return this.storage;
                }

                this.ResolveAndPrepareStorage();
                return this.storage;
            }
        }
    }

    void ICrystal.Configure(CrystalConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            if (configuration.FileConfiguration is RelativeFileConfiguration relativeFile)
            {
                configuration = configuration with { FileConfiguration = this.Crystalizer.GlobalMain.CombineFile(relativeFile.Path) };
            }

            if (this.Crystalizer.GlobalBackup is { } globalBackup)
            {
                if (configuration.BackupFileConfiguration == null)
                {
                    configuration = configuration with { BackupFileConfiguration = globalBackup.CombineFile(configuration.FileConfiguration.Path) };
                }

                if (configuration.StorageConfiguration.BackupDirectoryConfiguration == null)
                {
                    var storageConfiguration = configuration.StorageConfiguration with { BackupDirectoryConfiguration = globalBackup.CombineDirectory(configuration.StorageConfiguration.DirectoryConfiguration), };
                    configuration = configuration with { StorageConfiguration = storageConfiguration, };
                }
            }

            this.CrystalConfiguration = configuration;
            this.crystalFiler = null;
            this.storage = null;
            this.State = CrystalState.Initial;
        }
    }

    void ICrystal.ConfigureFile(FileConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.CrystalConfiguration = this.CrystalConfiguration with { FileConfiguration = configuration, };
            this.crystalFiler = null;
            this.State = CrystalState.Initial;
        }
    }

    void ICrystal.ConfigureStorage(StorageConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.CrystalConfiguration = this.CrystalConfiguration with { StorageConfiguration = configuration, };
            this.storage = null;
            this.State = CrystalState.Initial;
        }
    }

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
        if (this.CrystalConfiguration.SavePolicy == SavePolicy.Volatile)
        {
            return CrystalResult.Success;
        }

        var obj = Volatile.Read(ref this.obj);
        var filer = Volatile.Read(ref this.crystalFiler);
        var currentWaypoint = this.waypoint;

        if (this.State == CrystalState.Initial)
        {// Initial
            return CrystalResult.NotPrepared;
        }
        else if (this.State == CrystalState.Deleted)
        {// Deleted
            return CrystalResult.Deleted;
        }
        else if (obj == null || filer == null)
        {
            return CrystalResult.NotPrepared;
        }

        // RetrySave:
        var options = TinyhandSerializerOptions.Standard with { Plane = this.waypoint.NextPlane, };
        byte[] byteArray;
        if (this.CrystalConfiguration.SaveFormat == SaveFormat.Utf8)
        {
            byteArray = TinyhandSerializer.SerializeObjectToUtf8(obj, options);
        }
        else
        {
            byteArray = TinyhandSerializer.SerializeObject(obj, options);
        }

        var hash = FarmHash.Hash64(byteArray.AsSpan());
        if (hash == currentWaypoint.Hash)
        {// Identical data
            return CrystalResult.Success;
        }

        using (this.semaphore.Lock())
        {
            if (!this.waypoint.Equals(currentWaypoint))
            {// Waypoint changed
                // goto RetrySave;
                return CrystalResult.Success;
            }

            this.Crystalizer.UpdatePlane(this, ref this.waypoint, hash);
            currentWaypoint = this.waypoint;
        }

        var result = await filer.Save(byteArray, currentWaypoint).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {// Write error
            return result;
        }

        _ = filer.LimitNumberOfFiles();
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

            // Delete file
            this.ResolveAndPrepareFiler();
            await this.crystalFiler.DeleteAllAsync().ConfigureAwait(false);

            // Delete storage
            this.ResolveAndPrepareStorage();
            await this.storage.DeleteStorageAsync().ConfigureAwait(false);

            // Journal/Waypoint
            this.Crystalizer.RemovePlane(this.waypoint);
            this.waypoint = default;

            // Clear
            TinyhandSerializer.DeserializeObject(TinyhandSerializer.SerializeObject(TinyhandSerializer.ReconstructObject<TData>()), ref this.obj);
            // this.obj = default;
            // TinyhandSerializer.ReconstructObject<TData>(ref this.obj);

            this.State = CrystalState.Deleted;
        }

        this.Crystalizer.DeleteInternal(this);
        return CrystalResult.Success;
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

    ulong ICrystalInternal.GetPosition()
        => this.waypoint.JournalPosition;

    #endregion

    #region ITinyhandCrystal

    bool ITinyhandCrystal.TryGetJournalWriter(JournalType recordType, uint plane, out TinyhandWriter writer)
    {
        if (this.Crystalizer.Journal is not null)
        {
            this.Crystalizer.Journal.GetWriter(recordType, plane, out writer);
            return true;
        }
        else
        {
            writer = default;
            return false;
        }
    }

    ulong ITinyhandCrystal.AddJournal(in TinyhandWriter writer)
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

    bool ITinyhandCrystal.TryAddToSaveQueue()
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

    private async Task<CrystalResult> PrepareAndLoadInternal(bool useQuery)
    {// this.semaphore.Lock()
        CrystalResult result;
        var param = PrepareParam.New<TData>(this.Crystalizer, useQuery);

        // CrystalFiler
        if (this.crystalFiler == null)
        {
            this.crystalFiler = new(this.Crystalizer);
            result = await this.crystalFiler.PrepareAndCheck(param, this.CrystalConfiguration).ConfigureAwait(false);
            if (result.IsFailure())
            {
                return result;
            }
        }

        // Storage
        if (this.storage == null)
        {
            this.storage = this.Crystalizer.ResolveStorage(this.CrystalConfiguration.StorageConfiguration);
            result = await this.storage.PrepareAndCheck(param, this.CrystalConfiguration.StorageConfiguration, false).ConfigureAwait(false);
            if (result.IsFailure())
            {
                return result;
            }
        }

        // Object
        if (this.obj is not null)
        {
            return CrystalResult.Success;
        }

        var filer = Volatile.Read(ref this.crystalFiler);
        var configuration = this.CrystalConfiguration;

        // !!! EXIT !!!
        this.semaphore.Exit();
        (CrystalResult Result, TData? Data, Waypoint Waypoint) loadResult;
        try
        {
            loadResult = await LoadAndDeserializeNotInternal(filer, param, configuration).ConfigureAwait(false);
        }
        finally
        {
            this.semaphore.Enter();
        }

        // !!! ENTERED !!!
        if (this.obj is not null)
        {
            return CrystalResult.Success;
        }
        else if (loadResult.Result.IsFailure())
        {
            return loadResult.Result;
        }

        // Check journal position
        if (this.Crystalizer.Journal is { } journal)
        {
            if (loadResult.Waypoint.JournalPosition > journal.GetCurrentPosition())
            {
                var query = await param.Query.InconsistentJournal(this.CrystalConfiguration.FileConfiguration.Path).ConfigureAwait(false);
                if (query == AbortOrContinue.Abort)
                {
                    return CrystalResult.CorruptedData;
                }
                else
                {
                    journal.ResetJournal(loadResult.Waypoint.JournalPosition);
                }
            }
        }

        if (loadResult.Data is { } data)
        {// Loaded
            this.obj = data;
            this.waypoint = loadResult.Waypoint;

            this.Crystalizer.SetPlane(this, ref this.waypoint);
            this.SetCrystalAndPlane();

            this.State = CrystalState.Prepared;
            return CrystalResult.Success;
        }
        else
        {// Reconstruct
            this.ReconstructObject();

            this.State = CrystalState.Prepared;
            return CrystalResult.Success;
        }
    }

#pragma warning disable SA1204 // Static elements should appear before instance elements
    private static async Task<(CrystalResult Result, TData? Data, Waypoint Waypoint)> LoadAndDeserializeNotInternal(CrystalFiler filer, PrepareParam param, CrystalConfiguration configuration)
#pragma warning restore SA1204 // Static elements should appear before instance elements
    {
        param.RegisterConfiguration(configuration.FileConfiguration, out var newlyRegistered);

        // Load data
        var data = await filer.LoadLatest(param).ConfigureAwait(false);
        if (data.Result.IsFailure)
        {
            if (!newlyRegistered &&
                await param.Query.FailedToLoad(configuration.FileConfiguration, data.Result.Result).ConfigureAwait(false) == AbortOrContinue.Abort)
            {
                return (data.Result.Result, default, default);
            }

            return (CrystalResult.Success, default, default); // Reconstruct
        }

        // Deserialize
        TData? obj = default;
        try
        {
            if (configuration.SaveFormat == SaveFormat.Utf8)
            {
                try
                {
                    TinyhandSerializer.DeserializeObjectFromUtf8(data.Result.Data.Memory.Span, ref obj);
                }
                catch
                {// Maybe binary...
                    TinyhandSerializer.DeserializeObject(data.Result.Data.Memory.Span, ref obj);
                }
            }
            else
            {
                try
                {
                    TinyhandSerializer.DeserializeObject(data.Result.Data.Memory.Span, ref obj);
                }
                catch
                {// Maybe utf8...
                    TinyhandSerializer.DeserializeObjectFromUtf8(data.Result.Data.Memory.Span, ref obj);
                }
            }

            if (obj == null)
            {
                return (CrystalResult.Success, default, default); // Reconstruct
            }
        }
        catch
        {
            if (await param.Query.FailedToLoad(configuration.FileConfiguration, CrystalResult.DeserializeError).ConfigureAwait(false) == AbortOrContinue.Abort)
            {
                return (data.Result.Result, default, default);
            }

            return (CrystalResult.Success, default, default); // Reconstruct
        }
        finally
        {
            data.Result.Return();
        }

        return (CrystalResult.Success, obj, data.Waypoint);
    }

    [MemberNotNull(nameof(crystalFiler))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResolveAndPrepareFiler()
    {
        if (this.crystalFiler == null)
        {
            this.crystalFiler = new(this.Crystalizer);
            this.crystalFiler.PrepareAndCheck(PrepareParam.NoQuery<TData>(this.Crystalizer), this.CrystalConfiguration).Wait();
        }
    }

    [MemberNotNull(nameof(storage))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResolveAndPrepareStorage()
    {
        if (this.storage == null)
        {
            this.storage = this.Crystalizer.ResolveStorage(this.CrystalConfiguration.StorageConfiguration);
            this.storage.PrepareAndCheck(PrepareParam.NoQuery<TData>(this.Crystalizer), this.CrystalConfiguration.StorageConfiguration, false).Wait();
        }
    }

    [MemberNotNull(nameof(obj))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReconstructObject()
    {
        TinyhandSerializer.ReconstructObject<TData>(ref this.obj);

        byte[] byteArray;
        if (this.CrystalConfiguration.SaveFormat == SaveFormat.Utf8)
        {
            byteArray = TinyhandSerializer.SerializeObjectToUtf8(this.obj);
        }
        else
        {
            byteArray = TinyhandSerializer.SerializeObject(this.obj);
        }

        var hash = FarmHash.Hash64(byteArray);
        this.waypoint = default;
        this.Crystalizer.UpdatePlane(this, ref this.waypoint, hash);

        this.SetCrystalAndPlane();

        // Save immediately to fix the waypoint.
        _ = this.crystalFiler?.Save(byteArray, this.waypoint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCrystalAndPlane()
    {
        if (this.obj is ITinyhandJournal journalObject)
        {
            journalObject.Crystal = this;
            journalObject.CurrentPlane = this.waypoint.CurrentPlane;
        }
    }
}
