// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

public class NetTerminalServer : NetTerminal
{
    internal NetTerminalServer(Terminal terminal, NodeInformation nodeInformation, ulong gene)
        : base(terminal, nodeInformation, gene)
    {// NodeInformation: Managed
    }
}
