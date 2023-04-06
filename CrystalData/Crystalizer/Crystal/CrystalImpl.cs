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

    public CrystalConfiguration CrystalConfiguration { get; private set; }

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
                this.PrepareAndLoadInternal(null).Wait();
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

                this.filer ??= this.Crystalizer.ResolveFiler(this.CrystalConfiguration.FilerConfiguration);
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

                this.storage ??= this.Crystalizer.ResolveStorage(this.CrystalConfiguration.StorageConfiguration);
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
        }
    }

    void ICrystal.ConfigureFiler(FilerConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.CrystalConfiguration = this.CrystalConfiguration with { FilerConfiguration = configuration, };
            this.filer = null;
        }
    }

    async Task<CrystalStartResult> ICrystal.PrepareAndLoad(CrystalStartParam? param)
    {
        if (this.obj != null)
        {// Prepared
            return CrystalStartResult.Success;
        }

        using (this.semaphore.Lock())
        {
            return await this.PrepareAndLoadInternal(param).ConfigureAwait(false);
        }
    }

    async Task<CrystalResult> ICrystal.Save()
    {
        using (this.semaphore.Lock())
        {
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

            return await this.Filer.WriteAsync(0, new(byteArray)).ConfigureAwait(false);
        }
    }

    void ICrystal.Delete()
    {
        using (this.semaphore.Lock())
        {
            // Delete file
            this.Filer.Delete();

            // Clear
            this.CrystalConfiguration = CrystalConfiguration.Default;
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
            this.filer = null;
        }
    }

    void ICrystal.Terminate()
    {
    }

    #endregion

    protected async Task<CrystalStartResult> PrepareAndLoadInternal(CrystalStartParam? param)
    {// this.semaphore.Lock()
        if (this.obj != null)
        {
            return CrystalStartResult.Success;
        }

        param ??= CrystalStartParam.Default;
        if (this.CrystalConfiguration.FilerConfiguration is not EmptyFilerConfiguration)
        {
            var result = await this.PrepareAndLoadInternalFiler(param).ConfigureAwait(false);
            if (result != CrystalStartResult.Success)
            {
                return result;
            }
        }

        if (this.CrystalConfiguration.FilerConfiguration is not EmptyFilerConfiguration)
        {
            var result = await this.PrepareAndLoadInternalStorage(param).ConfigureAwait(false);
            if (result != CrystalStartResult.Success)
            {
                return result;
            }
        }

        return CrystalStartResult.Success;
    }

    private async Task<CrystalStartResult> PrepareAndLoadInternalFiler(CrystalStartParam param)
    {
        var result = await this.Filer.PrepareAndCheck(this.Crystalizer, this.CrystalConfiguration.FilerConfiguration).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return CrystalStartResult.DirectoryError;
        }

        // Load
        var memoryResult = await this.Filer.ReadAsync(0, -1).ConfigureAwait(false);
        if (!memoryResult.IsSuccess)
        {
            if (await param.Query(CrystalStartResult.FileNotFound).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                goto Reconstruct;
            }

            return CrystalStartResult.FileNotFound;
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
                goto Reconstruct;
            }

            return CrystalStartResult.DeserializeError;
        }
        finally
        {
            memoryResult.Return();
        }

Reconstruct:
        // Reconstruct
        TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
        this.savedHash = FarmHash.Hash64(TinyhandSerializer.SerializeObject<TData>(this.obj));
        return CrystalStartResult.Success;
    }

    private async Task<CrystalStartResult> PrepareAndLoadInternalStorage(CrystalStartParam param)
    {
        var result = await this.Storage.PrepareAndCheck(this.Crystalizer, this.CrystalConfiguration.StorageConfiguration, false).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return CrystalStartResult.DirectoryError;
        }

        return CrystalStartResult.Success;
    }
}
