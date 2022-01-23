// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public interface IServiceFilterBase
{
    public void SetArguments(object[] args)
    {
    }
}

public interface IServiceFilter : IServiceFilterBase
{
    public void Invoke(CallContext context, Action<CallContext> invoker);
}

public interface IServiceFilterAsync : IServiceFilterBase
{
    public Task Invoke(CallContext context, Func<CallContext, Task> invoker);
}

// Currently disabled.
/*public interface IServiceFilter<TCallContext> : IServiceFilterBase
    where TCallContext : CallContext
{
    public Task Invoke(TCallContext context, Func<TCallContext, Task> invoker);
}*/
