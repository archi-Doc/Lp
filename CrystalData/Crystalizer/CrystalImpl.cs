// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData;

internal class CrystalImpl<TData> : ICrystal<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    internal CrystalImpl(Crystalizer crystalizer, IFiler<TData> filer)
    {
        this.Crystalizer = crystalizer;
        this.Configuration = CrystalConfiguration.Default;
        this.Filer = filer;
    }

    #region FieldAndProperty

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration Configuration { get; private set; }

    private SemaphoreLock semaphore = new();
    private TData? obj;
    private IFilerToCrystal? filerToCrystal;

    #endregion

    #region ICrystal

    object ICrystal.Object => ((ICrystal<TData>)this).Object;

    public IFiler Filer { get; }

    TData ICrystal<TData>.Object
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

    void ICrystal.Configure(CrystalConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            // Release filerToCrystal
            this.ReleaseFilerToCrystalInternal();

            this.Configuration = configuration;
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
            this.filerToCrystal?.Filer.Delete(this.Configuration.FilerConfiguration.Path);

            // Release
            this.ReleaseFilerToCrystalInternal();

            // Clear
            this.Configuration = CrystalConfiguration.Default;
            TinyhandSerializer.ReconstructObject<T>(ref this.obj);
        }
    }

    #endregion

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDeleted()
    {
        if (this.deleted)
        {
            throw new InvalidOperationException("This object has already been deleted.");
        }
    }*/

    private async Task<CrystalStartResult> PrepareAndLoadInternal(CrystalStartParam? param)
    {// this.semaphore.Lock()
        var filerConfiguration = this.Configuration.FilerConfiguration;
        if (this.filerToCrystal == null)
        {
            this.filerToCrystal = this.Crystalizer.GetFilerToCrystal(this, filerConfiguration);
        }

        // Load
        var result = await this.filerToCrystal.Filer.ReadAsync(filerConfiguration.Path, 0, -1).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return CrystalStartResult.FileNotFound;
        }

        // Deserialize
        try
        {
            TinyhandSerializer.DeserializeObject<TData>(result.Data.Memory.Span, ref this.obj);
        }
        catch
        {
            return CrystalStartResult.DeserializeError;
        }
        finally
        {
            result.Return();
        }

        return CrystalStartResult.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReleaseFilerToCrystalInternal()
    {
        this.filerToCrystal?.Dispose();
        this.filerToCrystal = null;
    }
}
