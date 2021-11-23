// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Net;

namespace LP;

internal class Server
{
    public Server(Information information)
    {
        this.Information = information;
    }

    public void Process(NetTerminalServer terminal)
    {
        terminal.Receive();
    }

    public Information Information { get; }
}
