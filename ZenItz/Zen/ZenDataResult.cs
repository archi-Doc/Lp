// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public readonly struct ZenDataResult
{
    public ZenDataResult(ZenResult result, ByteArrayPool.ReadOnlyMemoryOwner data)
    {
        this.Result = result;
        this.Data = data;
    }

    public ZenDataResult(ZenResult result)
    {
        this.Result = result;
        this.Data = default;
    }

    public void Return() => this.Data.Return();

    public bool IsSuccess => this.Result == ZenResult.Success;

    public readonly ZenResult Result;

    public readonly ByteArrayPool.ReadOnlyMemoryOwner Data;
}
