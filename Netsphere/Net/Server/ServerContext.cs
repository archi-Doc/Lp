// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Server;

public class ServerContext
{
    public ServerContext()
    {
    }

    public IServiceProvider ServiceProvider { get; internal set; } = default!;

    public ServerTerminal Terminal { get; internal set; } = default!;
}
