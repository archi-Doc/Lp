// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable SA1401

namespace CrystalData;

public class CrystalImpl<TData> : ICrystal<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    internal CrystalImpl(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.CrystalConfiguration = crystalizer.GetCrystalConfiguration(typeof(TData));
    }

    #region FieldAndProperty

    protected SemaphoreLock semaphore = new();
    protected TData? obj;
    protected IFiler? filer;
    protected IStorage? storage;
    protected ulong savedHash;

    #endregion

    #region ICrystal

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration CrystalConfiguration { get; protected set; }

    object ICrystal.Object => ((ICrystal<TData>)this).Object;

    public TData Object
    {
        get
        {
            if (this.obj != null)
            {
                return this.obj;
            }

            using (this.semaphore.Lock())
            {
                // Load
                this.PrepareAndLoadInternal_IfNotPrepared();

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

    public bool Prepared { get; protected set; }

    public IFiler Filer
    {
        get
        {
            if (this.filer != null)
            {
                return this.filer;
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
            if (this.storage != null)
            {
                return this.storage;
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

    async Task<CrystalStartResult> ICrystal.PrepareAndLoad(CrystalStartParam? param)
    {
        using (this.semaphore.Lock())
        {
            if (this.Prepared)
            {// Prepared
                return CrystalStartResult.Success;
            }

            return await this.PrepareAndLoadInternal(param).ConfigureAwait(false);
        }
    }

    async Task<CrystalResult> ICrystal.Save(bool unload)
    {
        using (this.semaphore.Lock())
        {
            this.PrepareAndLoadInternal_IfNotPrepared();

            var byteArray = TinyhandSerializer.SerializeObject<TData>(this.obj);
            var hash = FarmHash.Hash64(byteArray.AsSpan());
            if (this.savedHash == hash)
            {// Identical
                return CrystalResult.Success;
            }
            else
            {
                this.savedHash = hash;
            }

            return await this.filer!.WriteAsync(0, new(byteArray)).ConfigureAwait(false);
        }
    }

    void ICrystal.Delete()
    {
        using (this.semaphore.Lock())
        {
            this.PrepareAndLoadInternal_IfNotPrepared();

            // Delete file/storage
            this.filer?.Delete();
            this.storage?.DeleteAllAsync();

            // Clear
            this.CrystalConfiguration = CrystalConfiguration.Default;
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
            this.filer = null;
            this.storage = null;
        }
    }

    void ICrystal.Terminate()
    {
    }

    #endregion

    protected virtual async Task<CrystalStartResult> PrepareAndLoadInternal(CrystalStartParam? param)
    {// this.semaphore.Lock()
        if (this.Prepared)
        {
            return CrystalStartResult.Success;
        }

        param ??= CrystalStartParam.Default;

        // Filer
        this.ResolveFiler();
        if (this.CrystalConfiguration.FileConfiguration is EmptyFileConfiguration)
        {
            this.Prepared = true;
            return CrystalStartResult.Success;
        }

        var result = await this.filer.PrepareAndCheck(this.Crystalizer, this.CrystalConfiguration.FileConfiguration).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            if (await param.Query(CrystalStartResult.FileNotFound).ConfigureAwait(false) == AbortOrComplete.Abort)
            {
                return CrystalStartResult.DirectoryError;
            }
        }

        // Load
        var memoryResult = await this.filer.ReadAsync(0, -1).ConfigureAwait(false);
        if (!memoryResult.IsSuccess)
        {
            if (await param.Query(CrystalStartResult.FileNotFound).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                ReconstructObject();
            }
            else
            {
                return CrystalStartResult.FileNotFound;
            }
        }

        // Deserialize
        try
        {
            TinyhandSerializer.DeserializeObject<TData>(memoryResult.Data.Memory.Span, ref this.obj);
            this.savedHash = FarmHash.Hash64(memoryResult.Data.Memory.Span);
        }
        catch
        {
            if (await param.Query(CrystalStartResult.FileNotFound).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                ReconstructObject();
            }
            else
            {
                return CrystalStartResult.DeserializeError;
            }
        }
        finally
        {
            memoryResult.Return();
        }

        // Storage
        this.ResolveStorage();
        result = await this.storage.PrepareAndCheck(this.Crystalizer, this.CrystalConfiguration.StorageConfiguration, false).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return CrystalStartResult.DirectoryError;
        }

        this.Prepared = true;
        return CrystalStartResult.Success;

        void ReconstructObject()
        {
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
            this.savedHash = FarmHash.Hash64(TinyhandSerializer.SerializeObject<TData>(this.obj));
        }
    }

    [MemberNotNull(nameof(obj))]
    protected virtual void ReconstructObject()
    {
        TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
    }

    [MemberNotNull(nameof(filer))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ResolveFiler()
    {
        this.filer ??= this.Crystalizer.ResolveFiler(this.CrystalConfiguration.FileConfiguration);
    }

    [MemberNotNull(nameof(filer))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ResolveAndPrepareFiler()
    {
        if (this.filer == null)
        {
            this.filer = this.Crystalizer.ResolveFiler(this.CrystalConfiguration.FileConfiguration);
            this.filer.PrepareAndCheck(this.Crystalizer, this.CrystalConfiguration.FileConfiguration).Wait();
        }
    }

    /*[MemberNotNull(nameof(filer))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async ValueTask ResolveAndPrepareFilerAsync()
    {
        if (this.filer == null)
        {
            this.filer = this.Crystalizer.ResolveFiler(this.CrystalConfiguration.FileConfiguration);
            await this.filer.PrepareAndCheck(this.Crystalizer, this.CrystalConfiguration.FileConfiguration).ConfigureAwait(false);
        }
    }*/

    [MemberNotNull(nameof(storage))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ResolveStorage()
    {
        this.storage ??= this.Crystalizer.ResolveStorage(this.CrystalConfiguration.StorageConfiguration);
    }

    [MemberNotNull(nameof(storage))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ResolveAndPrepareStorage()
    {
        if (this.storage == null)
        {
            this.storage = this.Crystalizer.ResolveStorage(this.CrystalConfiguration.StorageConfiguration);
            this.storage.PrepareAndCheck(this.Crystalizer, this.CrystalConfiguration.StorageConfiguration, false).Wait();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void PrepareAndLoadInternal_IfNotPrepared()
    {
        if (!this.Prepared)
        {
            this.PrepareAndLoadInternal(null).Wait();
        }
    }
}
