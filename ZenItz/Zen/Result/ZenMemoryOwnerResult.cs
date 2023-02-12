// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public readonly struct ZenMemoryOwnerResult
{
    public ZenMemoryOwnerResult(ZenResult result, ByteArrayPool.ReadOnlyMemoryOwner data)
    {
        this.Result = result;
        this.Data = data;
    }

    public ZenMemoryOwnerResult(ZenResult result)
    {
        this.Result = result;
        this.Data = default;
    }

    public void Return() => this.Data.Return();

    public bool IsSuccess => this.Result == ZenResult.Success;

    public readonly ZenResult Result;

    public readonly ByteArrayPool.ReadOnlyMemoryOwner Data;
}
