// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CrystalData.Filer;

namespace CrystalData;

public sealed class CrystalObject<TData> : ICrystal<TData>
    where TData : class, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
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

    #endregion

    #region ICrystal

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration CrystalConfiguration { get; private set; }

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
                TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
                return this.obj;
            }
        }
    }

    public bool Prepared { get; private set; }

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
            this.crystalFiler = null;
            this.Prepared = false;
        }
    }

    void ICrystal.ConfigureStorage(StorageConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.CrystalConfiguration = this.CrystalConfiguration with { StorageConfiguration = configuration, };
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
            if (!this.Prepared)
            {
                await this.PrepareAndLoadInternal(CrystalPrepare.ContinueAll).ConfigureAwait(false);
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

            this.Prepared = false;
            return CrystalResult.Success;
        }
    }

    void ICrystal.Terminate()
    {
    }

    #endregion

    private async Task<CrystalResult> PrepareAndLoadInternal(CrystalPrepare prepare)
    {// this.semaphore.Lock()
        CrystalResult result;
        var param = prepare.ToParam<TData>(this.Crystalizer);

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
    private static async Task<(CrystalResult Result, TData? Data, Waypoint Waypoint)> LoadAndDeserializeNotInternal(CrystalFiler filer, PrepareParam param, CrystalConfiguration configuration)
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

    [MemberNotNull(nameof(crystalFiler))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResolveAndPrepareFiler()
    {
        if (this.crystalFiler == null)
        {
            this.crystalFiler = new(this.Crystalizer);
            this.crystalFiler.PrepareAndCheck(PrepareParam.ContinueAll<TData>(this.Crystalizer), this.CrystalConfiguration).Wait();
        }
    }

    [MemberNotNull(nameof(storage))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResolveAndPrepareStorage()
    {
        if (this.storage == null)
        {
            this.storage = this.Crystalizer.ResolveStorage(this.CrystalConfiguration.StorageConfiguration);
            this.storage.PrepareAndCheck(PrepareParam.ContinueAll<TData>(this.Crystalizer), this.CrystalConfiguration.StorageConfiguration, false).Wait();
        }
    }
}
