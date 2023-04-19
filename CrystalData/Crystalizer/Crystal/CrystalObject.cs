// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CrystalData.Journal;

#pragma warning disable SA1401

namespace CrystalData;

// Data + Journal/Waypoint + Filer/FileConfiguration + Storage/StorageConfiguration
public class CrystalObject<TData> : ICrystal<TData>
    where TData : IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public CrystalObject(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.CrystalConfiguration = CrystalConfiguration.Default;
    }

    #region FieldAndProperty

    protected SemaphoreLock semaphore = new();
    protected TData? obj;
    protected IFiler? filer;
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
                    this.PrepareAndLoadInternal(CrystalPrepare.NoQuery).Wait();
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

    public IFiler Filer
    {
        get
        {
            if (this.filer is { } v)
            {
                return v;
            }

            using (this.semaphore.Lock())
            {
                if (this.filer != null)
                {
                    return this.filer;
                }

                this.ResolveAndPrepareFiler();
                return this.filer;
            }
        }
    }

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
            this.filer = null;
            this.storage = null;
            this.Prepared = false;
        }
    }

    void ICrystal.ConfigureFile(FileConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.CrystalConfiguration = this.CrystalConfiguration with { FileConfiguration = configuration, };
            this.filer = null;
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
        ulong hash;
        CrystalResult result;
        using (this.semaphore.Lock())
        {
            if (!this.Prepared || this.obj == null)
            {
                return CrystalResult.NotPrepared;
                /*if (await this.PrepareAndLoadInternal(null).ConfigureAwait(false) != CrystalStartResult.Success)
                {
                    return CrystalResult.NoData;
                }*/
            }

            if (this.Crystalizer.Journal is { } journal)
            {
                var newToken = journal.UpdateToken(this.waypoint.JournalToken, this.obj);
                this.waypoint = new(this.waypoint.JournalPosition, newToken, this.waypoint.Hash);
            }

            // var options = TinyhandSerializerOptions.Standard with { Token = this.waypoint.JournalToken, };
            var byteArray = TinyhandSerializer.SerializeObject(this.obj);
            hash = FarmHash.Hash64(byteArray.AsSpan());

            if (this.waypoint.Hash != hash)
            {// Save
                result = await this.filer!.WriteAsync(0, new(byteArray)).ConfigureAwait(false);
                if (result != CrystalResult.Success)
                {// Write error
                    return result;
                }
            }

            ulong journalPosition;
            if (this.Crystalizer.Journal != null)
            {
                journalPosition = AddJournal();
            }
            else
            {
                journalPosition = 0;
            }

            this.waypoint = new(journalPosition, this.waypoint.JournalToken, hash);

            var waypointFiler = this.filer!.CloneWithExtension(Waypoint.Extension);
            result = await waypointFiler.WriteAsync(0, new(this.waypoint.ToByteArray())).ConfigureAwait(false);
            return result;
        }

        ulong AddJournal()
        {
            this.Crystalizer.Journal.GetWriter(JournalRecordType.Waypoint, this.waypoint.JournalToken, out var writer);
            writer.Write(this.waypoint.JournalToken);
            writer.Write(hash);
            return this.Crystalizer.Journal.Add(writer);
        }
    }

    async Task<CrystalResult> ICrystal.Delete()
    {
        using (this.semaphore.Lock())
        {
            if (!this.Prepared)
            {
                await this.PrepareAndLoadInternal(CrystalPrepare.NoQuery).ConfigureAwait(false);
            }

            // Delete file/storage
            if (this.filer?.DeleteAsync() is { } task)
            {
                await task.ConfigureAwait(false);
            }

            if (this.storage?.DeleteAllAsync() is { } task2)
            {
                await task2.ConfigureAwait(false);
            }

            // Journal/Waypoint
            this.Crystalizer.Journal?.UnregisterToken(this.waypoint.JournalToken);
            this.waypoint = default;

            // Clear
            this.CrystalConfiguration = CrystalConfiguration.Default;
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
            this.filer = null;
            this.storage = null;

            this.Prepared = false;
            return CrystalResult.Success;
        }
    }

    void ICrystal.Terminate()
    {
    }

    #endregion

    protected virtual async Task<CrystalResult> PrepareAndLoadInternal(CrystalPrepare prepare)
    {// this.semaphore.Lock()
        if (this.Prepared)
        {
            return CrystalResult.Success;
        }

        var param = prepare.ToParam<TData>(this.Crystalizer);

        // Filer
        if (this.filer == null)
        {
            this.filer = this.Crystalizer.ResolveFiler(this.CrystalConfiguration.FileConfiguration);
            var result = await this.filer.PrepareAndCheck(param, this.CrystalConfiguration.FileConfiguration).ConfigureAwait(false);
            if (result.IsFailure())
            {
                return result;
            }
        }

        // Storage
        if (this.storage == null)
        {
            this.storage = this.Crystalizer.ResolveStorage(this.CrystalConfiguration.StorageConfiguration);
            var result = await this.storage.PrepareAndCheck(param, this.CrystalConfiguration.StorageConfiguration, false).ConfigureAwait(false);
            if (result.IsFailure())
            {
                return result;
            }
        }

        // Load data
        var data = await PathHelper.LoadData(this.filer).ConfigureAwait(false);
        if (data.Result.IsFailure)
        {
            if (await param.Query(this.CrystalConfiguration.FileConfiguration, data.Result.Result).ConfigureAwait(false) == AbortOrContinue.Abort)
            {
                return data.Result.Result;
            }

            return DataLost();
        }

        // Deserialize
        try
        {
            TinyhandSerializer.DeserializeObject(data.Result.Data.Memory.Span, ref this.obj);
            if (this.obj == null)
            {
                return DataLost();
            }
        }
        catch
        {
            if (await param.Query(this.CrystalConfiguration.FileConfiguration, CrystalResult.DeserializeError).ConfigureAwait(false) == AbortOrContinue.Abort)
            {
                return CrystalResult.DeserializeError;
            }

            return DataLost();
        }
        finally
        {
            data.Result.Return();
        }

        this.Crystalizer.Journal?.RegisterToken(this.waypoint.JournalToken, this.obj);

        this.Prepared = true;
        return CrystalResult.Success;

        CrystalResult DataLost()
        {
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
            var hash = FarmHash.Hash64(TinyhandSerializer.SerializeObject(this.obj));

            ulong journalPosition = 1;
            uint journalToken = 0;
            if (this.Crystalizer.Journal is { } journal)
            {
                journalToken = journal.NewToken(this.obj);
                journal.GetWriter(JournalRecordType.Waypoint, journalToken, out var writer);
                journalPosition = journal.Add(writer);
            }

            this.waypoint = new(journalPosition, journalToken, hash);

            this.Prepared = true;
            return CrystalResult.Success;
        }
    }

    [MemberNotNull(nameof(obj))]
    protected virtual void ReconstructObject(bool createNew)
    {
        if (this.obj == null || createNew)
        {
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
        }
    }

    [MemberNotNull(nameof(filer))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ResolveAndPrepareFiler()
    {
        if (this.filer == null)
        {
            this.filer = this.Crystalizer.ResolveFiler(this.CrystalConfiguration.FileConfiguration);
            this.filer.PrepareAndCheck(PrepareParam.NoQuery<TData>(this.Crystalizer), this.CrystalConfiguration.FileConfiguration).Wait();
        }
    }

    [MemberNotNull(nameof(storage))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ResolveAndPrepareStorage()
    {
        if (this.storage == null)
        {
            this.storage = this.Crystalizer.ResolveStorage(this.CrystalConfiguration.StorageConfiguration);
            this.storage.PrepareAndCheck(PrepareParam.NoQuery<TData>(this.Crystalizer), this.CrystalConfiguration.StorageConfiguration, false).Wait();
        }
    }
}
