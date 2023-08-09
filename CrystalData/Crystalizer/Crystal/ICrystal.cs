﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystal : ITinyhandJournal
{
    Crystalizer Crystalizer { get; }

    CrystalConfiguration CrystalConfiguration { get; }

    bool IsConfigured => this.CrystalConfiguration != CrystalConfiguration.Default;

    Type DataType { get; }

    object Data { get; }

    CrystalState State { get; }

    IStorage Storage { get; }

    void Configure(CrystalConfiguration configuration);

    void ConfigureFile(FileConfiguration configuration);

    void ConfigureStorage(StorageConfiguration configuration);

    Task<CrystalResult> PrepareAndLoad(bool useQuery);

    Task<CrystalResult> Save(bool unload = false);

    Task<CrystalResult> Delete();

    void Terminate();
}

public interface ICrystal<TData> : ICrystal
    where TData : class, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public new TData Data { get; }
}

internal interface ICrystalInternal : ICrystal
{
    Task? TryPeriodicSave(DateTime utc);

    Task TestJournal();

    ulong JournalPosition { get; set; }

    Waypoint Waypoint { get; }
}

internal interface ICrystalInternal<TData> : ICrystal<TData>, ICrystalInternal
    where TData : class, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}
