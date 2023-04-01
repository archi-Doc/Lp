// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IFiler
{
}

public interface IFiler<TData> : IFiler
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}

internal class FilerFactory<TData> : IFiler<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public FilerFactory(Crystalizer crystalizer)
    {
        crystalizer.ThrowIfNotRegistered<TData>();

        this.Crystalizer = crystalizer;
    }

    public Crystalizer Crystalizer { get; }
}
