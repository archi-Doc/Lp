// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IBigCrystal : ICrystal
{
    BigCrystalConfiguration BigCrystalConfiguration { get; }

    DatumRegistry DatumRegistry { get; }

    StorageGroup StorageGroup { get; }

    HimoGoshujinClass Himo { get; }

    long MemoryUsage { get; }

    void Configure(BigCrystalConfiguration configuration);
}

public interface IBigCrystal<TData> : IBigCrystal, ICrystal<TData>
    where TData : BaseData, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}
