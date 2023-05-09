// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystal
{
    Crystalizer Crystalizer { get; }

    CrystalConfiguration CrystalConfiguration { get; }

    Type ObjectType { get; }

    object Object { get; }

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
    where TData : class, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public new TData Object { get; }
}

internal interface ICrystalInternal : ICrystal
{
    Task? TryPeriodicSave(DateTime utc);
}

internal interface ICrystalInternal<TData> : ICrystal<TData>, ICrystalInternal
    where TData : class, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}
