// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace CrystalData;

internal class CrystalImpl<T> : ICrystal<T>
    where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
{
    internal CrystalImpl(Crystalizer crystalizer)
    {
        this.crystalizer = crystalizer;
    }

    public void Setup()
    {
    }

    object ICrystalBase.Object => ((ICrystal<T>)this).Object;

    T ICrystal<T>.Object
    {
        get
        {
            if (this.obj == null)
            {
                this.PrepareObject();
            }

            return this.obj;
        }
    }

    void ICrystal<T>.Configure(CrystalConfiguration configuration)
    {
    }

    [MemberNotNull(nameof(obj))]
    private void PrepareObject()
    {
        if (this.obj != null)
        {
            return;
        }

        TinyhandSerializer.ReconstructObject<T>(ref this.obj);
    }

    private Crystalizer crystalizer;
    private object syncObject = new();
    private T? obj;
}
