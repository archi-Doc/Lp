// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

internal class CrystalDataImpl<TData> : CrystalImpl<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public CrystalDataImpl(Crystalizer crystalizer)
        : base(crystalizer)
    {
    }
}
