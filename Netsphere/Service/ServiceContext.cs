﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class ServiceContext
{
    public static ServiceContext Current => currentServiceContext.Value ?? (currentServiceContext.Value = new());

    public ServiceContext()
    {
    }

    private static AsyncLocal<ServiceContext> currentServiceContext = new AsyncLocal<ServiceContext>();
}

/*public class ServiceContext : ServiceContext<ServiceContext>
{
    public ServiceContext()
    {
    }
}

public abstract class ServiceContext<T>
    where T : ServiceContext<T>, new()
{
    public int A { get; set; }
}*/
