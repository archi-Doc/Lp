// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public abstract class NetServiceFilterAttribute : Attribute
{
    public int Order
    {
        get => this.order;
        set => this.order = value;
    }

    public NetServiceFilterAttribute()
    {
        this.order = int.MaxValue;
    }

    public abstract ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next);

    private int order;
}
