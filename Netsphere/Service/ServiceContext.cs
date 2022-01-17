// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Netsphere;

public class ServiceContext
{
    public ServiceContext()
    {
    }

    public IServiceProvider ServiceProvider { get; internal set; } = default!;

    public ConcurrentDictionary<Type, IServiceFilterBase> ServiceFilters { get; } = new();
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
