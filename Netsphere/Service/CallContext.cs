// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Netsphere;

#pragma warning disable SA1401 // Fields should be private

public class CallContext
{
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

    private object syncObject = new();
    private ConcurrentDictionary<string, object>? items;
}
