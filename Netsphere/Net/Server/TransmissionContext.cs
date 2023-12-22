// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Server;

public class TransmissionContext
{
    public TransmissionContext(ServerContext serverContext)
    {
        this.ServerContext = serverContext;
    }

    public ServerContext ServerContext { get; }
}
