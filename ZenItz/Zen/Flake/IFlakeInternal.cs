// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IFlakeInternal
{
    IZenInternal ZenInternal { get; }

    DataConstructor Data { get; }

    ZenOptions Options { get; }

    void DataToStorage<TData>(ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        where TData : IData;

    Task<ZenMemoryOwnerResult> StorageToData<TData>()
        where TData : IData;

    void DeleteStorage<TData>()
        where TData : IData;

    void SaveData(int id, bool unload);
}
