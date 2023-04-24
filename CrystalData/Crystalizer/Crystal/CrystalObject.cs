// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CrystalData.Filer;

#pragma warning disable SA1401

namespace CrystalData;

// Data + Journal/Waypoint + Filer/FileConfiguration + Storage/StorageConfiguration
public class CrystalObject<TData> : ICrystal<TData>
    where TData : class, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public CrystalObject(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.CrystalConfiguration = CrystalConfiguration.Default;
    }

    #region FieldAndProperty

    protected SemaphoreLock semaphore = new();
    protected TData? obj;
    protected CrystalFiler? crystalFiler;
    protected IStorage? storage;
    protected Waypoint waypoint;

    #endregion

    #region ICrystal

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration CrystalConfiguration { get; protected set; }

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
                // Prepare and load
                if (!this.Prepared)
                {
                    this.PrepareAndLoadInternal(CrystalPrepare.ContinueAll).Wait();
                }

                if (this.obj != null)
                {
                    return this.obj;
                }

                // Finally, reconstruct
                this.ReconstructObject(false);
                return this.obj;
            }
        }
    }

    public bool Prepared { get; protected set; }

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
            this.CrystalConfiguration = configuration;
            this.crystalFiler = null;
            this.storage = null;
            this.Prepared = false;
        }
    }

    void ICrystal.ConfigureFile(FileConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.CrystalConfiguration = this.CrystalConfiguration with { FileConfiguration = configuration, };
            /*var newConfiguration = this.CrystalConfiguration with { };
            newConfiguration.ConfigureInternal(configuration, newConfiguration.StorageConfiguration);
            this.CrystalConfiguration = newConfiguration;*/

            this.crystalFiler = null;
            this.Prepared = false;
        }
    }

    void ICrystal.ConfigureStorage(StorageConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.CrystalConfiguration = this.CrystalConfiguration with { StorageConfiguration = configuration, };
            /*var newConfiguration = this.CrystalConfiguration with { };
            newConfiguration.ConfigureInternal(newConfiguration.FileConfiguration, configuration);
            this.CrystalConfiguration = newConfiguration;*/

            this.storage = null;
            this.Prepared = false;
        }
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

    async Task<CrystalResult> ICrystal.Save(bool unload)
    {
        var obj = Volatile.Read(ref this.obj);
        var filer = Volatile.Read(ref this.crystalFiler);
        var currentWaypoint = this.waypoint;

        if (!this.Prepared || obj == null || filer == null)
        {
            return CrystalResult.NotPrepared;
        }

        // RetrySave:
        // var options = TinyhandSerializerOptions.Standard with { Token = this.waypoint.NextPlane, };
        byte[] byteArray;
        if (this.CrystalConfiguration.SaveFormat == SaveFormat.Utf8)
        {
            byteArray = TinyhandSerializer.SerializeObjectToUtf8(obj);
        }
        else
        {
            byteArray = TinyhandSerializer.SerializeObject(obj);
        }

        var hash = FarmHash.Hash64(byteArray.AsSpan());

        if (currentWaypoint.Hash == hash)
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
            if (!this.Prepared)
            {
                await this.PrepareAndLoadInternal(CrystalPrepare.ContinueAll).ConfigureAwait(false);
            }

            // Delete file/storage
            if (this.crystalFiler?.DeleteAllAsync() is { } task)
            {
                await task.ConfigureAwait(false);
            }

            if (this.storage?.DeleteAllAsync() is { } task2)
            {
                await task2.ConfigureAwait(false);
            }

            // Journal/Waypoint
            this.Crystalizer.RemovePlane(this.waypoint);
            this.waypoint = default;

            // Clear
            this.CrystalConfiguration = CrystalConfiguration.Default;
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
            this.crystalFiler = null;
            this.storage = null;

            this.Prepared = false;
            return CrystalResult.Success;
        }
    }

    void ICrystal.Terminate()
    {
    }

    #endregion

    protected virtual async Task DeleteAllInternal()
    {
        if (this.crystalFiler is { } filer)
        {
            await filer.DeleteAllAsync().ConfigureAwait(false);
            this.crystalFiler = null;
        }

        this.ReconstructObject(true);
    }

    protected virtual async Task<CrystalResult> PrepareAndLoadInternal(CrystalPrepare prepare)
    {// this.semaphore.Lock()
        if (this.Prepared)
        {
            return CrystalResult.Success;
        }

        CrystalResult result;
        var param = prepare.ToParam<TData>(this.Crystalizer);

        if (prepare.CreateNew)
        {
            await this.DeleteAllInternal();
            this.ReconstructObject(true);

            this.Prepared = true;
            return CrystalResult.Success;
        }

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

        if (loadResult.Data is { } data)
        {// Loaded
            this.obj = data;
            this.waypoint = loadResult.Waypoint;

            this.Crystalizer.SetPlane(this, ref this.waypoint);

            this.Prepared = true;
            return CrystalResult.Success;
        }
        else
        {// Reconstruct
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
            var hash = FarmHash.Hash64(TinyhandSerializer.SerializeObject(this.obj));
            this.waypoint = default;

            this.Crystalizer.UpdatePlane(this, ref this.waypoint, hash);

            this.Prepared = true;
            return CrystalResult.Success;
        }
    }

#pragma warning disable SA1204 // Static elements should appear before instance elements
    protected static async Task<(CrystalResult Result, TData? Data, Waypoint Waypoint)> LoadAndDeserializeNotInternal(CrystalFiler filer, PrepareParam param, CrystalConfiguration configuration)
#pragma warning restore SA1204 // Static elements should appear before instance elements
    {
        // Load data
        var data = await filer.LoadLatest().ConfigureAwait(false);
        if (data.Result.IsFailure)
        {
            if (await param.Query(configuration.FileConfiguration, data.Result.Result).ConfigureAwait(false) == AbortOrContinue.Abort)
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
                TinyhandSerializer.DeserializeObjectFromUtf8(data.Result.Data.Memory.Span, ref obj);
            }
            else
            {
                TinyhandSerializer.DeserializeObject(data.Result.Data.Memory.Span, ref obj);
            }

            if (obj == null)
            {
                return (CrystalResult.Success, default, default); // Reconstruct
            }
        }
        catch
        {
            if (await param.Query(configuration.FileConfiguration, CrystalResult.DeserializeError).ConfigureAwait(false) == AbortOrContinue.Abort)
            {
                return (CrystalResult.DeserializeError, default, default);
            }

            return (CrystalResult.Success, default, default); // Reconstruct
        }
        finally
        {
            data.Result.Return();
        }

        return (CrystalResult.Success, obj, data.Waypoint);
    }

    [MemberNotNull(nameof(obj))]
    protected virtual void ReconstructObject(bool createNew)
    {// this.semaphore.Lock()
        if (this.obj == null || createNew)
        {
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
        }
    }

    /*[MemberNotNull(nameof(rawFiler))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ResolveAndPrepareFiler()
    {
        if (this.rawFiler == null)
        {
            this.rawFiler = this.Crystalizer.ResolveFiler(this.CrystalConfiguration.FileConfiguration);
            this.rawFiler.PrepareAndCheck(PrepareParam.ContinueAll<TData>(this.Crystalizer), this.CrystalConfiguration.FileConfiguration).Wait();
        }
    }*/

    [MemberNotNull(nameof(storage))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ResolveAndPrepareStorage()
    {
        if (this.storage == null)
        {
            this.storage = this.Crystalizer.ResolveStorage(this.CrystalConfiguration.StorageConfiguration);
            this.storage.PrepareAndCheck(PrepareParam.ContinueAll<TData>(this.Crystalizer), this.CrystalConfiguration.StorageConfiguration, false).Wait();
        }
    }
}
