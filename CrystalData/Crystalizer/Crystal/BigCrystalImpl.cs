// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

internal class BigCrystalImpl<TData> : CrystalImpl<TData>, IBigCrystal<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public BigCrystalImpl(Crystalizer crystalizer)
        : base(crystalizer)
    {
        this.BigCrystalConfiguration = crystalizer.GetCrystalConfiguration(typeof(TData));
        this.BigCrystalConfiguration.RegisterDatum(this.DatumRegistry);
        this.storageGroup = new(crystalizer);
        this.himoGoshujin = new(this);
    }

    public BigCrystalConfiguration BigCrystalConfiguration { get; }

    public DatumRegistry DatumRegistry { get; } = new();

    public StorageGroup StorageGroup => this.storageGroup;

    public HimoGoshujinClass Himo => this.himoGoshujin;

    public long MemoryUsage => this.himoGoshujin.MemoryUsage;

    private StorageGroup storageGroup;

    private HimoGoshujinClass himoGoshujin;
}
