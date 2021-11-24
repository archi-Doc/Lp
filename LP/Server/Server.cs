// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Net;

namespace LP;

internal class Server
{
    public Server(Information information, Netsphere netsphere)
    {
        this.Information = information;
        this.Netsphere = netsphere;
    }

    public void Process(NetTerminalServer terminal)
    {
        // terminal.Receive();
    }

    public ThreadCoreBase? Core => this.Netsphere.Terminal.Core;

    public Information Information { get; }

    public Netsphere Netsphere { get; }
}
