// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ZenItz.Crystal.Core;

namespace CrystalData;

public interface IFlakeInternal
{
    ICrystalInternal ZenInternal { get; }

    DataConstructor Data { get; }

    CrystalOptions Options { get; }

    void DataToStorage<TData>(ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        where TData : IDatum;

    Task<CrystalMemoryOwnerResult> StorageToData<TData>()
        where TData : IDatum;

    void DeleteStorage<TData>()
        where TData : IDatum;

    void SaveData(int id, bool unload);
}
