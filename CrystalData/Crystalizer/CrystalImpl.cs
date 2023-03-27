// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace CrystalData;

internal class CrystalImpl<T> : ICrystal<T>
    where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
{
    public CrystalImpl(CrystalizerClass crystalizer)
    {
        this.crystalizer = crystalizer;
    }

    public void Setup()
    {
    }

    public T Object
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

    [MemberNotNull(nameof(obj))]
    private void PrepareObject()
    {
        if (this.obj != null)
        {
            return;
        }

        TinyhandSerializer.ReconstructObject<T>(ref this.obj);
    }

    private CrystalizerClass crystalizer;
    private object syncObject = new();
    private T? obj;
}
