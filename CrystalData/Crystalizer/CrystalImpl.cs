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

    public CrystalConfiguration Configuration { get; }

    private object syncObject = new();
    private T? obj;

    #endregion

    object ICrystalBase.Object => ((ICrystal<T>)this).Object;

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

    void ICrystal<T>.Configure(CrystalConfiguration configuration)
    {
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
