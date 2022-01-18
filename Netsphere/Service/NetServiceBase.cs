// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class NetServiceBase
{
    public ServerContext Context { get; set; } = default!;
}

public class NetServiceBase<TServerContext>
    where TServerContext : ServerContext, new()
{
    public TServerContext Context { get; set; } = default!;
}
