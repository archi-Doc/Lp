// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace xUnitTest.NetsphereTest;

public class NullFilter : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        context.Result = NetResult.NoNetService;
    }
}
