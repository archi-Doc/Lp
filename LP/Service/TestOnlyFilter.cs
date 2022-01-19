// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace LP.Services;

public class TestOnlyFilter : IServiceFilter
{
    public TestOnlyFilter(NetBase netBase)
    {
        this.NetBase = netBase;
    }

    public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        if (this.NetBase.NetsphereOptions.EnableTestFeatures)
        {
            await invoker(context);
        }
        else
        {
            context.Result = NetResult.NoNetService;
        }
    }

    public NetBase NetBase { get; }
}
