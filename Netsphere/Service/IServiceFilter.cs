// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public interface IServiceFilterBase
{
}

public interface IServiceFilter : IServiceFilterBase
{
    public ValueTask Invoke(CallContext context, Func<ServiceContext, ValueTask> next);
}

public interface IServiceFilter<TServiceContext> : IServiceFilterBase
    where TServiceContext : ServiceContext
{
    public ValueTask Invoke(CallContext<TServiceContext> context, Func<TServiceContext, ValueTask> next);
}
