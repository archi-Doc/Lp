// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere;

public class ClientConnectionContext
{
    public ClientConnectionContext(ClientConnection clientConnection)
    {
        this.Connection = clientConnection;
    }

    public ClientConnection Connection { get; }

    public bool IsAuthenticated
        => this.AuthenticationToken is not null;

    public AuthenticationToken? AuthenticationToken { get; set; }
}
