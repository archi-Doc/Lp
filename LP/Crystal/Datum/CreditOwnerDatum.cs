// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;

namespace LP.Crystal;

public interface CreditOwnerDatum : IDatum
{
    const ushort Id = 2;

    static ushort IDatum.StaticId => Id;
}

internal class CreditOwnerDatumImpl : HimoGoshujinClass.Himo, CreditOwnerDatum, IBaseDatum
{
    public CreditOwnerDatumImpl(IDataInternal flakeInternal)
        : base(flakeInternal)
    {
    }

    public override ushort Id => BlockDatum.Id;

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
