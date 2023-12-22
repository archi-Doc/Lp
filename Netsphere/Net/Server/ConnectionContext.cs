// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Server;

public class ConnectionContext
{
    public ConnectionContext(IServiceProvider serviceProvider, ServerConnection serverConnection)
    {
        this.ServiceProvider = serviceProvider;
        this.ServerConnection = serverConnection;
    }

    public IServiceProvider ServiceProvider { get; internal set; } = default!;

    public ServerConnection ServerConnection { get; internal set; } = default!;
}
