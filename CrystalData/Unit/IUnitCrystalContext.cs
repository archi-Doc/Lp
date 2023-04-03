// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IUnitCrystalContext
{
    void AddCrystal<TData>(CrystalConfiguration crystalConfiguration)
        where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>;

    void AddBigCrystal<TData>(BigCrystalConfiguration crystalConfiguration, CrystalConfiguration dataConfiguration)
        where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>;
}
