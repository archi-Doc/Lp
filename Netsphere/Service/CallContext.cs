// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Netsphere;

public class CallContext
{
    public CallContext(ServiceContext serviceContext, ByteArrayPool.MemoryOwner rentData)
    {
        this.ServiceContext = serviceContext;
        this.RentData = rentData;
    }

    public ServiceContext ServiceContext { get; }

    public ByteArrayPool.MemoryOwner RentData { get; set; }

    public NetResult Result { get; set; }
}
