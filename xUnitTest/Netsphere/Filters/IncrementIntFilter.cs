// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.Server;
using Tinyhand;

namespace xUnitTest.NetsphereTest;

public class IncrementIntFilter : IServiceFilter
{
    public async Task Invoke(TransmissionContext context, Func<TransmissionContext, Task> invoker)
    {
        if (TinyhandSerializer.TryDeserialize<int>(context.Owner.Memory.Span, out var value))
        {
            if (NetHelper.TrySerialize(value + 1, out var owner))
            {
                context.Owner.Return();
                context.Owner = owner;
            }
        }

        await invoker(context).ConfigureAwait(false);
    }
}
