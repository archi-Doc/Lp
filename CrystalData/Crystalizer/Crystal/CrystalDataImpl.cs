// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

internal class CrystalDataImpl<TData> : CrystalImpl<TData>, IBigCrystal<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public CrystalDataImpl(Crystalizer crystalizer)
        : base(crystalizer)
    {
        this.CrystalConfiguration = crystalizer.GetCrystalConfiguration(typeof(TData));
        this.CrystalConfiguration.RegisterDatum(this.DatumRegistry);
        this.himoGoshujin = new(this);
    }

    public CrystalConfiguration CrystalConfiguration { get; }

    public DatumRegistry DatumRegistry { get; } = new();

    public HimoGoshujinClass Himo => this.himoGoshujin;

    public long MemoryUsage => this.himoGoshujin.MemoryUsage;

    private HimoGoshujinClass himoGoshujin;
}
