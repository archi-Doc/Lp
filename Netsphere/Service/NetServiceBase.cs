// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class NetServiceBase
{
    public ServiceContext Context { get; set; } = default!;
}

public class NetServiceBase<TServiceContext>
    where TServiceContext : ServiceContext, new()
{
    public TServiceContext Context { get; set; } = default!;
}
