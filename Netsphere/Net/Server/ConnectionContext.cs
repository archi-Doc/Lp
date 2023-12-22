// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Server;

public class ConnectionContext
{
    public ConnectionContext()
    {
    }

    public IServiceProvider ServiceProvider { get; internal set; } = default!;

    public ServerConnection Connection { get; internal set; } = default!;
}
