// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IUnitCrystalContext
{
    void AddCrystal<TData>(CrystalConfiguration configuration)
        where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>;

    void AddBigCrystal<TData>(BigCrystalConfiguration configuration)
        where TData : BaseData, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>;

    void SetJournal(JournalConfiguration configuration);
}
