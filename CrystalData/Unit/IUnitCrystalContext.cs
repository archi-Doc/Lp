// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IUnitCrystalContext
{
    void AddData<TData>(DataConfiguration crystalConfiguration)
        where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>;

    void AddCrystalData<TData>(CrystalConfiguration crystalConfiguration, DataConfiguration dataConfiguration)
        where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>;
}
