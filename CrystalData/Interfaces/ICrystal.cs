// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystal
{
    object Object { get; }

    IFiler Filer { get; }

    public void Configure(CrystalConfiguration configuration);

    Task<CrystalStartResult> PrepareAndLoad(CrystalStartParam? param = null);

    Task Save();

    Task Delete();
}

public interface ICrystal<TData> : ICrystal
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public new TData Object { get; }
}

public class CrystalNotRegistered<TData> : ICrystal<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public CrystalNotRegistered()
    {
        Crystalizer.ThrowTypeNotRegistered(typeof(TData));
    }

    TData ICrystal<TData>.Object => throw new NotImplementedException();

    IFiler ICrystal.Filer => throw new NotImplementedException();

    object ICrystal.Object => throw new NotImplementedException();

    void ICrystal.Configure(CrystalConfiguration configuration) => throw new NotImplementedException();

    Task<CrystalStartResult> ICrystal.PrepareAndLoad(CrystalStartParam? param) => throw new NotImplementedException();

    Task ICrystal.Save() => throw new NotImplementedException();

    Task ICrystal.Delete() => throw new NotImplementedException();
}
