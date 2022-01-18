// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public interface IServiceFilterBase
{
}

public interface IServiceFilter : IServiceFilterBase
{
    public Task Invoke(CallContext context, Func<ServerContext, Task> next);
}

public interface IServiceFilter<TCallContext> : IServiceFilterBase
    where TCallContext : CallContext
{
    public Task Invoke(TCallContext context, Func<TCallContext, Task> next);
}
