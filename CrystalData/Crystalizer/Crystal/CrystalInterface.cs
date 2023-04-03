﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;

namespace CrystalData;

public interface ICrystal
{
    Crystalizer Crystalizer { get; }

    CrystalOptions Options => this.Crystalizer.Options;

    CrystalConfiguration CrystalConfiguration { get; }

    object Object { get; }

    IFiler Filer { get; }

    IStorage Storage { get; }

    public void Configure(CrystalConfiguration configuration);

    public void ConfigureFiler(FilerConfiguration configuration);

    Task<CrystalStartResult> PrepareAndLoad(CrystalStartParam? param = null);

    Task<CrystalResult> Save();

    void Delete();
}

public interface ICrystal<TData> : ICrystal
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public new TData Object { get; }
}

/*internal class CrystalFactory<TData> : ICrystal<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}*/