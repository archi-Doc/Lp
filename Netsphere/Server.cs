// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Net;

namespace LP.Net;

public class Server
{
    public Server(NetBase netBase, NetControl netControl)
    {
        this.NetBase = netBase;
        this.NetControl = netControl;
    }

    public void Process(NetTerminalServer terminal)
    {
        // terminal.Receive();
    }

    public ThreadCoreBase? Core => this.NetControl.Terminal.Core;

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }
}
