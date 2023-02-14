// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface BlockData : IData
{
    static int IData.StaticId => 1;

    static object IData.StaticNew() => new object();

    void SetSpan(ReadOnlySpan<byte> data, bool clearSavedFlag);

    void SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj, bool clearSavedFlag);
}

internal class BlockDataImpl : BlockData, BaseData
{
    static int IData.StaticId => 1;

    // static object IData.StaticNew() => new BlockDataImpl();

    public int Id => 1;

    public void SaveInternal(bool unload)
    {
    }

    public void SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj, bool clearSavedFlag)
    {
    }

    public void SetSpan(ReadOnlySpan<byte> data, bool clearSavedFlag)
    {
    }
}
