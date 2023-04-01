﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData;

internal class CrystalImpl<TData> : ICrystal<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    internal CrystalImpl(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.Configuration = CrystalConfiguration.Default;
    }

    #region FieldAndProperty

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration Configuration { get; private set; }

    private SemaphoreLock semaphore = new();
    private TData? obj;
    private IFiler? filer;

    #endregion

    #region ICrystal

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
                if (this.obj != null)
                {
                    return this.obj;
                }

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

                this.filer = this.Crystalizer.ResolveFiler(this.Configuration.FilerConfiguration);
                return this.filer;
            }
        }
    }

    void ICrystal.Configure(CrystalConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.Configuration = configuration;
            this.filer = null;
        }
    }

    void ICrystal.ConfigureFiler(FilerConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.Configuration = this.Configuration with { FilerConfiguration = configuration, };
            this.filer = null;
        }
    }

    async Task<CrystalStartResult> ICrystal.PrepareAndLoad(CrystalStartParam? param)
    {
        using (this.semaphore.Lock())
        {
            return await this.PrepareAndLoadInternal(param).ConfigureAwait(false);
        }
    }

    async Task ICrystal.Save()
    {
    }

    async Task ICrystal.Delete()
    {
        using (this.semaphore.Lock())
        {
            // Delete file
            this.Filer.Delete();

            // Clear
            this.Configuration = CrystalConfiguration.Default;
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
            this.filer = null;
        }
    }

    #endregion

    private async Task<CrystalStartResult> PrepareAndLoadInternal(CrystalStartParam? param)
    {// this.semaphore.Lock()
        this.filer ??= this.Crystalizer.ResolveFiler(this.Configuration.FilerConfiguration);
        if (this.Configuration.FilerConfiguration is EmptyFilerConfiguration)
        {
            return CrystalStartResult.Success;
        }

        var result = await this.filer.PrepareAndCheck(this.Crystalizer).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return CrystalStartResult.DirectoryError;
        }

        // Load
        var memoryResult = await this.filer.ReadAsync(0, -1).ConfigureAwait(false);
        if (!memoryResult.IsSuccess)
        {
            return CrystalStartResult.FileNotFound;
        }

        // Deserialize
        try
        {
            TinyhandSerializer.DeserializeObject<TData>(memoryResult.Data.Memory.Span, ref this.obj);
        }
        catch
        {
            return CrystalStartResult.DeserializeError;
        }
        finally
        {
            memoryResult.Return();
        }

        return CrystalStartResult.Success;
    }
}
