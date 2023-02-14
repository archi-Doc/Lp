// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface BlockData : IData
{
    static int IData.StaticId => 1;

    void SetSpan(ReadOnlySpan<byte> data, bool clearSavedFlag);

    void SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj, bool clearSavedFlag);
}

internal class BlockDataImpl : BlockData, BaseData
{
    public BlockDataImpl(ZenOptions options, IFromDataToIO fromDataToIO)
    {
        this.options = options;
        this.fromDataToIO = fromDataToIO;
    }

    public void SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj, bool clearSavedFlag)
    {
    }

    public void SetSpan(ReadOnlySpan<byte> data, bool clearSavedFlag)
    {
    }

    private ZenOptions options;
    private IFromDataToIO fromDataToIO;
}
