// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;

namespace LP.T3CS;

public interface CreditOwnerDatum : IDatum
{
    const int Id = 2;

    static int IDatum.StaticId => Id;
}

internal class CreditOwnerDatumImpl : HimoGoshujinClass.Himo, CreditOwnerDatum, IBaseDatum
{
    public CreditOwnerDatumImpl(IDataInternal flakeInternal)
        : base(flakeInternal)
    {
    }

    public override int Id => BlockDatum.Id;

    private bool isSaved = true;

    void IBaseDatum.Save()
    {
        if (!this.isSaved)
        {// Not saved.
            this.isSaved = true;
        }
    }

    void IBaseDatum.Unload()
    {
        this.RemoveHimo();
    }
}
