// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Server;

namespace Netsphere;

public abstract class NetServiceBase
{
    public NetServiceBase(ConnectionContext connectionContext)
    {
        this.ConnectionContext = connectionContext;
    }

    public ConnectionContext ConnectionContext { get; }
}

public abstract class NetServiceBase<TConnectionContext>
    where TConnectionContext : ConnectionContext
{
    public NetServiceBase(TConnectionContext connectionContext)
    {
        this.ConnectionContext = connectionContext;
    }

    public TConnectionContext ConnectionContext { get; }
}
