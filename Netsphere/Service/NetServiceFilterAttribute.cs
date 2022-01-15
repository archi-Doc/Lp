// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class NetServiceFilterAttribute : Attribute
{
    public int Order
    {
        get => this.order;
        set => this.order = value;
    }

    public NetServiceFilterAttribute(Type filterType)
    {
        this.order = int.MaxValue;
    }

    private int order;
}

public interface IServiceFilter
{
    public ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next);
}

public interface IServiceFilter<T>
    where T : ServiceContext
{
    public ValueTask Invoke(T context, Func<T, ValueTask> next);
}
