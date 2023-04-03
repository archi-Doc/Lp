// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IBigCrystal : ICrystal
{
    CrystalConfiguration CrystalConfiguration { get; }

    DatumRegistry DatumRegistry { get; }

    HimoGoshujinClass Himo { get; }

    long MemoryUsage { get; }
}

public interface IBigCrystal<TData> : IBigCrystal, ICrystal<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}
