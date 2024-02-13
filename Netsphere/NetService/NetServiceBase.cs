// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Server;

namespace Netsphere;

public abstract class NetServiceBase
{
    public NetServiceBase(ServerConnectionContext connectionContext)
    {
        this.ConnectionContext = connectionContext;
    }

    public ServerConnectionContext ConnectionContext { get; }
}

public abstract class NetServiceBase<TConnectionContext>
    where TConnectionContext : ServerConnectionContext
{
    public NetServiceBase(TConnectionContext connectionContext)
    {
        this.ConnectionContext = connectionContext;
    }

    public TConnectionContext ConnectionContext { get; }
}
