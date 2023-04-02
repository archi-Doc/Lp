// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystal
{
    object Object { get; }

    IFiler Filer { get; }

    public DataConfiguration Configuration { get; }

    public void Configure(DataConfiguration configuration);

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
