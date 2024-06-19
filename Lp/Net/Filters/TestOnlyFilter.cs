// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace Lp.NetServices;

public class TestOnlyFilter : IServiceFilter
{
    public TestOnlyFilter(LpBase lpBase)
    {
        this.LpBase = lpBase;
    }

    public async Task Invoke(TransmissionContext context, Func<TransmissionContext, Task> invoker)
    {
        if (this.LpBase.Options.TestFeatures)
        {
            await invoker(context);
        }
        else
        {
            context.Result = NetResult.NoNetService;
        }
    }

    public LpBase LpBase { get; }
}
