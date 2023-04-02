// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

internal class CrystalDataImpl<TData> : CrystalImpl<TData>, ICrystalData<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public CrystalDataImpl(Crystalizer crystalizer)
        : base(crystalizer)
    {
        this.CrystalConfiguration = crystalizer.GetCrystalConfiguration(typeof(TData));
        this.CrystalConfiguration.RegisterDatum(this.DatumRegistry);
    }

    public CrystalConfiguration CrystalConfiguration { get; }

    public DatumRegistry DatumRegistry { get; } = new();
}
