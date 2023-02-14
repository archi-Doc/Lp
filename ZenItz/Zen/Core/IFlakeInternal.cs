// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IFlakeInternal
{
    IZenInternal ZenInternal { get; }

    ZenOptions Options { get; }

    void SaveInternal<TData>(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        where TData : IData;

    Task<ZenMemoryOwnerResult> LoadInternal<TData>()
        where TData : IData;

    void RemoveInternal<TData>()
        where TData : IData;

    void Unload(int id);
}
