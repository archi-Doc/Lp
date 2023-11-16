// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Tinyhand;

namespace xUnitTest.NetsphereTest;

public class MultiplyIntFilter : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        if (TinyhandSerializer.TryDeserialize<int>(context.RentData.Memory.Span, out var value))
        {
            if (Netsphere.Block.BlockService.TrySerialize(value * this.multiplier, out var owner))
            {
                context.RentData.Return();
                context.RentData = owner;
            }
        }

        await invoker(context).ConfigureAwait(false);
    }

    public void SetArguments(object[] args)
    {
        if (args[0] is int x)
        {
            this.multiplier = x;
        }
    }

    private int multiplier = 2;
}
