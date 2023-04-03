// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CrystalData;

internal class CrystalImpl<TData> : ICrystal<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    internal CrystalImpl(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.DataConfiguration = CrystalConfiguration.Default;
    }

    #region FieldAndProperty

    private SemaphoreLock semaphore = new();
    private TData? obj;
    private IFiler? filer;
    private ulong savedHash;

    #endregion

    #region ICrystal

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration DataConfiguration { get; private set; }

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

                this.ResolveFiler();
                return this.filer;
            }
        }
    }

    void ICrystal.Configure(CrystalConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.DataConfiguration = configuration;
            this.filer = null;
        }
    }

    void ICrystal.ConfigureFiler(FilerConfiguration configuration)
    {
        using (this.semaphore.Lock())
        {
            this.DataConfiguration = this.DataConfiguration with { FilerConfiguration = configuration, };
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

            this.ResolveFiler();
            return await this.filer.WriteAsync(0, new(byteArray)).ConfigureAwait(false);
        }
    }

    void ICrystal.Delete()
    {
        using (this.semaphore.Lock())
        {
            // Delete file
            this.ResolveFiler();
            this.filer.Delete();

            // Clear
            this.DataConfiguration = CrystalConfiguration.Default;
            TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
            this.filer = null;
        }
    }

    #endregion

    private async Task<CrystalStartResult> PrepareAndLoadInternal(CrystalStartParam? param)
    {// this.semaphore.Lock()
        if (this.obj != null)
        {
            return CrystalStartResult.Success;
        }

        param ??= CrystalStartParam.Default;

        this.ResolveFiler();
        if (this.DataConfiguration.FilerConfiguration is EmptyFilerConfiguration)
        {
            return CrystalStartResult.Success;
        }

        var result = await this.filer.PrepareAndCheck(this.Crystalizer, this.DataConfiguration.FilerConfiguration).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return CrystalStartResult.DirectoryError;
        }

        // Load
        var memoryResult = await this.filer.ReadAsync(0, -1).ConfigureAwait(false);
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

        return CrystalStartResult.Success;

Reconstruct:
// Reconstruct
        TinyhandSerializer.ReconstructObject<TData>(ref this.obj);
        this.savedHash = FarmHash.Hash64(TinyhandSerializer.SerializeObject<TData>(this.obj));
        return CrystalStartResult.Success;
    }

    [MemberNotNull(nameof(filer))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResolveFiler()
    {
        this.filer ??= this.Crystalizer.ResolveFiler(this.DataConfiguration.FilerConfiguration);
    }
}
