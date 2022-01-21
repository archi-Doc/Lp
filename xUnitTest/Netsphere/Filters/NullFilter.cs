// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace xUnitTest.Netsphere;

public class NullFilter : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        context.Result = NetResult.NoNetService;
    }
}
