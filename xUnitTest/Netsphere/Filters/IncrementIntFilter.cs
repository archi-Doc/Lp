// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Tinyhand;

namespace xUnitTest.NetsphereTest;

public class IncrementIntFilter : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        if (TinyhandSerializer.TryDeserialize<int>(context.RentData.Memory.Span, out var value))
        {
            if (LP.Block.BlockService.TrySerialize(value + 1, out var owner))
            {
                context.RentData.Return();
                context.RentData = owner;
            }
        }

        await invoker(context).ConfigureAwait(false);
    }
}
