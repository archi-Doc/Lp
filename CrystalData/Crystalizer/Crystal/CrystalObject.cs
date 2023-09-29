// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CrystalData.Filer;
using CrystalData.Storage;
using Tinyhand.IO;

namespace CrystalData;

public sealed class CrystalObject<TData> : ICrystalInternal<TData>, IJournalObject
    where TData : class, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{// Data + Journal/Waypoint + Filer/FileConfiguration + Storage/StorageConfiguration
    public CrystalObject(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.CrystalConfiguration = CrystalConfiguration.Default;
        ((IJournalObject)this).Journal = this;
    }

    #region FieldAndProperty

    private SemaphoreLock semaphore = new();
    private TData? data;
    private CrystalFiler? crystalFiler;
    private IStorage? storage;
    private Waypoint waypoint;
    private DateTime lastSavedTime;

    #endregion

    #region ICrystal

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration CrystalConfiguration { get; private set; }

    public Type DataType => typeof(TData);

    object ICrystal.Data => ((ICrystal<TData>)this).Data!;

    public TData Data
    {
        get
        {
            if (this.data is { } v)
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
                    TinyhandSerializer.ReconstructObject<TData>(ref this.data);
                    this.SetJournal();
                    return this.data;
                }

                if (this.data != null)
                {
                    return this.data;
                }

                // Finally, reconstruct
                this.ResetWaypoint(true);
                return this.data;
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

        var obj = Volatile.Read(ref this.data);
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

        if (this.storage is { } storage && storage is not EmptyStorage)
        {
            await storage.SaveStorage();
        }

        this.lastSavedTime = DateTime.UtcNow;

        // Starting position
        var startingPosition = this.Crystalizer.GetJournalPosition();

        // Serialize
        byte[] byteArray;
        if (this.CrystalConfiguration.SaveFormat == SaveFormat.Utf8)
        {
            byteArray = TinyhandSerializer.SerializeObjectToUtf8(obj);
        }
        else
        {
            byteArray = TinyhandSerializer.SerializeObject(obj);
        }

        // Get hash
        var hash = FarmHash.Hash64(byteArray.AsSpan());
        if (hash == currentWaypoint.Hash)
        {// Identical data
            goto Exit;
        }

        var waypoint = this.waypoint;
        if (!waypoint.Equals(currentWaypoint))
        {// Waypoint changed
            goto Exit;
        }

        this.Crystalizer.UpdateWaypoint(this, ref currentWaypoint, hash, startingPosition);

        var result = await filer.Save(byteArray, currentWaypoint).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {// Write error
            return result;
        }

        using (this.semaphore.Lock())
        {// Update waypoint and plane position.
            this.waypoint = currentWaypoint;
            this.Crystalizer.CrystalCheck.SetShortcutPosition(currentWaypoint, startingPosition);
            if (unload)
            {
                this.data = null;
                this.State = CrystalState.Initial;
            }
        }

        _ = filer.LimitNumberOfFiles();
        return CrystalResult.Success;

Exit:
        using (this.semaphore.Lock())
        {
            this.Crystalizer.CrystalCheck.SetShortcutPosition(currentWaypoint, startingPosition);
            if (unload)
            {
                this.data = null;
                this.State = CrystalState.Initial;
            }
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
            TinyhandSerializer.DeserializeObject(TinyhandSerializer.SerializeObject(TinyhandSerializer.ReconstructObject<TData>()), ref this.data);
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

        var elapsed = utc - this.lastSavedTime;
        if (elapsed < this.CrystalConfiguration.SaveInterval)
        {
            return null;
        }

        this.lastSavedTime = utc;
        return ((ICrystal)this).Save(false);
    }

    Waypoint ICrystalInternal.Waypoint
        => this.waypoint;

    ITinyhandJournal? IJournalObject.Journal { get; set; }

    IJournalObject? IJournalObject.JournalParent { get; set; } = null;

    int IJournalObject.JournalKey { get; set; } = -1;

    /*void IJournalObject.WriteLocator(ref Tinyhand.IO.TinyhandWriter writer)
    {
        writer.Write_Locator();
        writer.Write(this.waypoint.Plane);
    }*/

    async Task<bool> ICrystalInternal.TestJournal()
    {
        if (this.Crystalizer.Journal is not CrystalData.Journal.SimpleJournal journal)
        {// No journaling
            return true;
        }

        var testResult = true;
        using (this.semaphore.Lock())
        {
            if (this.crystalFiler is null ||
                this.crystalFiler.Main is not { } main)
            {
                return testResult;
            }

            var waypoints = main.GetWaypoints();
            if (waypoints.Length <= 1)
            {// The number of waypoints is 1 or less.
                return testResult;
            }

            var logger = this.Crystalizer.UnitLogger.GetLogger<TData>();
            TData? previousObject = default;
            for (var i = 0; i < waypoints.Length; i++)
            {// waypoint[i] -> waypoint[i + 1]
                var base32 = waypoints[i].ToBase32();

                // Load
                var result = await main.LoadWaypoint(waypoints[i]).ConfigureAwait(false);
                if (result.IsFailure)
                {// Loading error
                    logger.TryGet(LogLevel.Error)?.Log(CrystalDataHashed.TestJournal.LoadingFailure, base32);
                    testResult = false;
                    break;
                }

                // Deserialize
                (var currentObject, var currentFormat) = TryDeserialize(result.Data.Span, this.CrystalConfiguration.SaveFormat);
                if (currentObject is null)
                {// Deserialization error
                    result.Return();
                    logger.TryGet(LogLevel.Error)?.Log(CrystalDataHashed.TestJournal.DeserializationFailure, base32);
                    testResult = false;
                    break;
                }

                if (previousObject is not null)
                {// Compare the previous data
                    bool compare;
                    if (currentFormat == SaveFormat.Binary)
                    {// Previous (previousObject), Current (currentObject/result.Data.Span): Binary
                        compare = result.Data.Span.SequenceEqual(TinyhandSerializer.Serialize(previousObject));
                    }
                    else
                    {// Previous (previousObject), Current (currentObject/result.Data.Span): Utf8
                        compare = result.Data.Span.SequenceEqual(TinyhandSerializer.SerializeToUtf8(previousObject));
                    }

                    if (compare)
                    {// Success
                        logger.TryGet(LogLevel.Information)?.Log(CrystalDataHashed.TestJournal.Success, base32);
                    }
                    else
                    {// Failure
                        logger.TryGet(LogLevel.Error)?.Log(CrystalDataHashed.TestJournal.Failure, base32);
                        testResult = false;
                    }
                }

                result.Return();
                if (i == waypoints.Length - 1)
                {
                    break;
                }

                if (currentObject is not IJournalObject journalObject)
                {
                    break;
                }

                // journalObject.CurrentPlane = waypoints[i].CurrentPlane;

                // Read journal [waypoints[i].JournalPosition, waypoints[i + 1].JournalPosition)
                var length = (int)(waypoints[i + 1].JournalPosition - waypoints[i].JournalPosition);
                var memoryOwner = ByteArrayPool.Default.Rent(length).ToMemoryOwner(0, length);
                var journalResult = await journal.ReadJournalAsync(waypoints[i].JournalPosition, waypoints[i + 1].JournalPosition, memoryOwner.Memory).ConfigureAwait(false);
                if (!journalResult)
                {// Journal error
                    testResult = false;
                    break;
                }

                this.ReadJournal(journalObject, memoryOwner.Memory, waypoints[i].Plane);

                previousObject = currentObject;
            }
        }

        return testResult;
    }

    #endregion

    #region ITinyhandJournal

    bool ITinyhandJournal.TryGetJournalWriter(JournalType recordType, out TinyhandWriter writer)
    {
        if (this.Crystalizer.Journal is not null)
        {
            this.Crystalizer.Journal.GetWriter(recordType, out writer);

            writer.Write_Locator();
            writer.Write(this.waypoint.Plane);
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

    private static (TData? Data, SaveFormat Format) TryDeserialize(ReadOnlySpan<byte> span, SaveFormat formatHint)
    {
        TData? data = default;
        SaveFormat format = SaveFormat.Binary;

        if (span.Length == 0)
        {// Empty
            data = TinyhandSerializer.ReconstructObject<TData>();
            return (data, format);
        }

        if (formatHint == SaveFormat.Utf8)
        {
            try
            {
                TinyhandSerializer.DeserializeObjectFromUtf8(span, ref data);
                format = SaveFormat.Utf8;
            }
            catch
            {// Maybe binary...
                data = default;
                try
                {
                    TinyhandSerializer.DeserializeObject(span, ref data);
                }
                catch
                {
                    data = default;
                }
            }
        }
        else
        {
            try
            {
                TinyhandSerializer.DeserializeObject(span, ref data);
            }
            catch
            {// Maybe utf8...
                data = default;
                try
                {
                    TinyhandSerializer.DeserializeObjectFromUtf8(span, ref data);
                    format = SaveFormat.Utf8;
                }
                catch
                {
                    data = default;
                }
            }
        }

        return (data, format);
    }

    private bool ReadJournal(IJournalObject journalObject, ReadOnlyMemory<byte> data, uint currentPlane)
    {
        var reader = new TinyhandReader(data.Span);
        var success = true;

        while (reader.Consumed < data.Length)
        {
            if (!reader.TryReadRecord(out var length, out var journalType))
            {
                return false;
            }

            var fork = reader.Fork();
            try
            {
                if (journalType == JournalType.Record)
                {
                    reader.Read_Locator();
                    var plane = reader.ReadUInt32();

                    if (plane == currentPlane)
                    {
                        if (journalObject.ReadRecord(ref reader))
                        {// Success
                        }
                        else
                        {// Failure
                            success = false;
                        }
                    }
                }
                else
                {
                }
            }
            catch
            {
                success = false;
            }
            finally
            {
                reader = fork;
                reader.Advance(length);
            }
        }

        return success;
    }

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

        // Data
        if (this.data is not null)
        {
            this.State = CrystalState.Prepared;
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
        if (this.data is not null)
        {
            return CrystalResult.Success;
        }
        else if (loadResult.Result.IsFailure())
        {
            return loadResult.Result;
        }

        // Check journal position
        if (loadResult.Waypoint.IsValid && this.Crystalizer.Journal is { } journal)
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
            this.data = data;
            this.waypoint = loadResult.Waypoint;
            if (this.waypoint.IsValid)
            {// Valid waypoint
                this.Crystalizer.SetPlane(this, ref this.waypoint);
                this.SetJournal();
            }
            else
            {// Invalid waypoint
                this.ResetWaypoint(false);
            }

            // this.LogWaypoint("Load");
            this.State = CrystalState.Prepared;
            return CrystalResult.Success;
        }
        else
        {// Reconstruct
            this.ResetWaypoint(true);

            // this.LogWaypoint("Reconstruct");
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
                configuration.Required &&
                await param.Query.FailedToLoad(configuration.FileConfiguration, data.Result.Result).ConfigureAwait(false) == AbortOrContinue.Abort)
            {
                return (data.Result.Result, default, default);
            }

            return (CrystalResult.Success, default, default); // Reconstruct
        }

        // Deserialize
        try
        {
            var deserializeResult = TryDeserialize(data.Result.Data.Memory.Span, configuration.SaveFormat);
            if (deserializeResult.Data == null)
            {
                if (configuration.Required &&
                    await param.Query.FailedToLoad(configuration.FileConfiguration, CrystalResult.DeserializeError).ConfigureAwait(false) == AbortOrContinue.Abort)
                {
                    return (data.Result.Result, default, default);
                }

                return (CrystalResult.Success, default, default); // Reconstruct
            }

            return (CrystalResult.Success, deserializeResult.Data, data.Waypoint);
        }
        finally
        {
            data.Result.Return();
        }
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

    [MemberNotNull(nameof(data))]
    private void ResetWaypoint(bool reconstruct)
    {
        if (reconstruct || this.data is null)
        {
            TinyhandSerializer.ReconstructObject<TData>(ref this.data);
        }

        byte[] byteArray;
        if (this.CrystalConfiguration.SaveFormat == SaveFormat.Utf8)
        {
            byteArray = TinyhandSerializer.SerializeObjectToUtf8(this.data);
        }
        else
        {
            byteArray = TinyhandSerializer.SerializeObject(this.data);
        }

        var hash = FarmHash.Hash64(byteArray);
        this.waypoint = default;
        this.Crystalizer.UpdateWaypoint(this, ref this.waypoint, hash, 0);

        this.SetJournal();

        // Save immediately to fix the waypoint.
        _ = this.crystalFiler?.Save(byteArray, this.waypoint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetJournal()
    {
        if (this.data is IJournalObject journalObject)
        {
            journalObject.Journal = this;
            journalObject.SetParent(this);
            // journalObject.CurrentPlane = this.waypoint.CurrentPlane;
        }
    }

    private void LogWaypoint(string prefix)
    {
        var logger = this.Crystalizer.UnitLogger.GetLogger<TData>();
        logger.TryGet(LogLevel.Error)?.Log($"{prefix}, {this.waypoint.ToString()}");
    }
}
