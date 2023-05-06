// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IBigCrystal : ICrystal
{
    BigCrystalConfiguration BigCrystalConfiguration { get; }

    DatumRegistry DatumRegistry { get; }

    StorageGroup StorageGroup { get; }

    void Configure(BigCrystalConfiguration configuration);
}

public interface IBigCrystal<TData> : IBigCrystal, ICrystal<TData>
    where TData : BaseData, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}

internal interface IBigCrystalInternal : IBigCrystal, ICrystalInternal
{
}

internal interface IBigCrystalInternal<TData> : IBigCrystal<TData>, IBigCrystalInternal
    where TData : BaseData, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}
