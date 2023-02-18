// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IFlakeInternal
{
    IZenInternal ZenInternal { get; }

    DataConstructor Data { get; }

    ZenOptions Options { get; }

    void DataToStorage<TData>(ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        where TData : IDatum;

    Task<CrystalMemoryOwnerResult> StorageToData<TData>()
        where TData : IDatum;

    void DeleteStorage<TData>()
        where TData : IDatum;

    void SaveData(int id, bool unload);
}
