// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class ClientConnectionContext
{
    public ClientConnectionContext(ClientConnection clientConnection)
    {
        this.Connection = clientConnection;
    }

    public ClientConnection Connection { get; }

    public bool IsAuthenticated { get; set; }
}
