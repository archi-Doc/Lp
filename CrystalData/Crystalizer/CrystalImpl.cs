// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace CrystalData;

internal class CrystalImpl<T> : ICrystal<T>
    where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
{
    internal CrystalImpl(Crystalizer crystalizer)
    {
        this.Crystalizer = crystalizer;
        this.Configuration = CrystalConfiguration.Default;
    }

    #region FieldAndProperty

    public Crystalizer Crystalizer { get; }

    public CrystalConfiguration Configuration { get; private set; }

    private object syncObject = new();
    private T? obj;

    #endregion

    #region Implementation

    object ICrystal.Object => ((ICrystal<T>)this).Object;

    T ICrystal<T>.Object
    {
        get
        {
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
        this.Configuration = configuration;
    }

    async Task<CrystalStartResult> ICrystal.PrepareAndLoad(CrystalStartParam? param)
    {
        return CrystalStartResult.Success;
    }

    async Task ICrystal.Save()
    {
    }

    #endregion

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
