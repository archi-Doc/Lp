// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystal
{
    object Object { get; }

    public void Configure(CrystalConfiguration configuration);

    Task<CrystalStartResult> PrepareAndLoad(CrystalStartParam? param = null);

    Task Save();
}

public interface ICrystal<T> : ICrystal
    where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
{
    public new T Object { get; }
}

public class CrystalNotRegistered<T> : ICrystal<T>
    where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
{
    public CrystalNotRegistered()
    {
        Crystalizer.ThrowTypeNotRegistered(typeof(T));
    }

    T ICrystal<T>.Object => throw new NotImplementedException();

    object ICrystal.Object => throw new NotImplementedException();

    void ICrystal.Configure(CrystalConfiguration configuration) => throw new NotImplementedException();

    Task<CrystalStartResult> ICrystal.PrepareAndLoad(CrystalStartParam? param) => throw new NotImplementedException();
}
