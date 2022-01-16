// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public interface IServiceFilter
{
    public ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next);
}

public interface IServiceFilter<TServiceContext>
    where TServiceContext : ServiceContext
{
    public ValueTask Invoke(TServiceContext context, Func<TServiceContext, ValueTask> next);
}
