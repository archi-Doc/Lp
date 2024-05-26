// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Tinyhand;

namespace xUnitTest.NetsphereTest;

public class MultiplyIntFilter : IServiceFilter
{
    public async Task Invoke(TransmissionContext context, Func<TransmissionContext, Task> invoker)
    {
        if (TinyhandSerializer.TryDeserialize<int>(context.RentMemory.Memory.Span, out var value))
        {
            if (NetHelper.TrySerialize(value * this.multiplier, out var owner))
            {
                context.RentMemory.Return();
                context.RentMemory = owner;
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
