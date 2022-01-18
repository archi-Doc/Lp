// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Netsphere;

#pragma warning disable SA1401 // Fields should be private

public class CallContext<TServerContext> : CallContext
    where TServerContext : ServerContext
{
    public CallContext()
    {
    }

    public new TServerContext ServiceContext => (TServerContext)base.ServiceContext;
}

public class CallContext
{
    public static CallContext Current => CurrentCallContext.Value!;

    public CallContext()
    {
    }

    public ServerContext ServiceContext { get; private set; } = default!;

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

    internal void Initialize(ServerContext serviceContext, ByteArrayPool.MemoryOwner rentData)
    {
        this.ServiceContext = serviceContext;
        this.RentData = rentData;
    }

    internal static AsyncLocal<CallContext?> CurrentCallContext = new();
    private object syncObject = new();
    private ConcurrentDictionary<string, object>? items;
}
