// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public readonly struct ServiceResponse
{
    public ServiceResponse(NetResult result)
    {
        this.result = result;
    }

    public NetResult Result => this.result;

    private readonly NetResult result;
}

public readonly struct ServiceResponse<T>
{
    public ServiceResponse(T value)
        : this(value, default)
    {
    }

    public ServiceResponse(T value, NetResult result)
    {
        this.value = value;
        this.result = result;
    }

    public T Value => this.value;

    public NetResult Result => this.result;

    private readonly T value;
    private readonly NetResult result;
}
