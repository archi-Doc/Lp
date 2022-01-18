// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Netsphere;

#pragma warning disable SA1401 // Fields should be private

public class LPServerContext : ServiceContext
{
}

public class LPCallContext : CallContext<LPServerContext>
{
    public LPCallContext(LPServerContext serviceContext, ByteArrayPool.MemoryOwner rentData)
        : base(serviceContext, rentData)
    {
    }
}

public class CallContext<TServerContext> : CallContext
    where TServerContext : ServiceContext
{
    public static new CallContext<TServerContext> Current => CurrentCallContext.Value!;

    public CallContext(TServerContext serviceContext, ByteArrayPool.MemoryOwner rentData)
        : base(serviceContext, rentData)
    {
        this.ServiceContext = serviceContext;
    }

    public new TServerContext ServiceContext { get; }

    internal static new AsyncLocal<CallContext<TServerContext>?> CurrentCallContext = new();
}

public class CallContext
{
    public static CallContext Current => CurrentCallContext.Value!;

    public CallContext(ServiceContext serviceContext, ByteArrayPool.MemoryOwner rentData)
    {
        this.ServiceContext = serviceContext;
        this.RentData = rentData;
    }

    public ServiceContext ServiceContext { get; }

    public ByteArrayPool.MemoryOwner RentData;

    public NetResult Result;

    public ConcurrentDictionary<string, object> Items
    {
        get
        {
            lock (this.syncObject)
            {
                this.items ??= new();
            }

            return this.items;
        }
    }

    internal static AsyncLocal<CallContext?> CurrentCallContext = new();
    private object syncObject = new();
    private ConcurrentDictionary<string, object>? items;
}
