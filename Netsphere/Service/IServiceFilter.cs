// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public interface IServiceFilterBase
{
}

public class TestFilter : IServiceFilter<LPCallContext>
{
    public ValueTask Invoke(LPCallContext context, Func<LPServerContext, ValueTask> next)
    {
        throw new NotImplementedException();
    }
}

public interface IServiceFilter : IServiceFilterBase
{
    public ValueTask Invoke(CallContext context, Func<ServiceContext, ValueTask> next);
}

public interface IServiceFilter<TCallContext> : IServiceFilterBase
    where TCallContext : CallContext
{
    public ValueTask Invoke(TCallContext context, Func<TCallContext, ValueTask> next);
}
