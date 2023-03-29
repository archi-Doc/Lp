// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CrystalData;

internal class CrystalImpl<T> : ICrystal<T>
    where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
{
    public enum State
    {
        Initial,
        Prepared,
        Deleted,
    }

    internal CrystalImpl(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.Configuration = CrystalConfiguration.Default;
    }

    #region FieldAndProperty

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration Configuration { get; private set; }

    private bool deleted = false;
    private object syncObject = new();
    private T? obj;

    #endregion

    #region ICrystal

    object ICrystal.Object => ((ICrystal<T>)this).Object;

    T ICrystal<T>.Object
    {
        get
        {
            this.ThrowIfDeleted();
            if (this.obj != null)
            {
                return this.obj;
            }

            this.PrepareObject();
            return this.obj;
        }
    }

    void ICrystal.Configure(CrystalConfiguration configuration)
    {
        this.ThrowIfDeleted();

        this.Configuration = configuration;
    }

    async Task<CrystalStartResult> ICrystal.PrepareAndLoad(CrystalStartParam? param)
    {
        this.ThrowIfDeleted();

        var filerConfiguration = this.Configuration.FilerConfiguration;
        var filer = this.Crystalizer.GetFiler(filerConfiguration);
        var result = await filer.ReadAsync(filerConfiguration.Path, 0, -1).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return CrystalStartResult.FileNotFound;
        }

        TinyhandSerializer.DeserializeObject<T>(result.Data.Memory.Span, ref this.obj);
        result.Return();

        return CrystalStartResult.Success;
    }

    async Task ICrystal.Save()
    {
        this.ThrowIfDeleted();
    }

    async Task ICrystal.Delete()
    {
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDeleted()
    {
        if (this.deleted)
        {
            throw new InvalidOperationException("This object has already been deleted.");
        }
    }

    [MemberNotNull(nameof(obj))]
    private void PrepareObject()
    {
        lock (this.syncObject)
        {
            if (this.obj != null)
            {
                return;
            }

            TinyhandSerializer.ReconstructObject<T>(ref this.obj);
        }
    }
}
