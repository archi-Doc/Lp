// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public readonly struct ZenMemoryResult
{
    public ZenMemoryResult(ZenResult result, ReadOnlyMemory<byte> data)
    {
        this.Result = result;
        this.Data = data;
    }

    public ZenMemoryResult(ZenResult result)
    {
        this.Result = result;
        this.Data = default;
    }

    public bool IsSuccess => this.Result == ZenResult.Success;

    public readonly ZenResult Result;

    public readonly ReadOnlyMemory<byte> Data;
}
