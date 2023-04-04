// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystal
{
    Crystalizer Crystalizer { get; }

    CrystalConfiguration CrystalConfiguration { get; }

    object Object { get; }

    IFiler Filer { get; }

    IStorage Storage { get; }

    void Configure(CrystalConfiguration configuration);

    void ConfigureFiler(FilerConfiguration configuration);

    Task<CrystalStartResult> PrepareAndLoad(CrystalStartParam? param = null);

    Task<CrystalResult> Save();

    void Delete();

    void Terminate();
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
