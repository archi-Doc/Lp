// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public readonly struct ZenObjectResult<T>
{
    public ZenObjectResult(ZenResult result, T obj)
    {
        this.Result = result;
        this.Object = obj;
    }

    public ZenObjectResult(ZenResult result)
    {
        this.Result = result;
        this.Object = default;
    }

    public bool IsSuccess => this.Result == ZenResult.Success;

    public readonly ZenResult Result;

    public readonly T? Object;
}
