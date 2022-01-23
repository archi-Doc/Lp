// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Netsphere;

public class ServerContext
{
    public ServerContext()
    {
    }

    public IServiceProvider ServiceProvider { get; internal set; } = default!;
}
