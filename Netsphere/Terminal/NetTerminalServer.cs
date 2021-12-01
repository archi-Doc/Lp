// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class NetTerminalServer : NetTerminal
{
    internal NetTerminalServer(Terminal terminal, NodeInformation nodeInformation, ulong gene)
        : base(terminal, nodeInformation, gene)
    {// NodeInformation: Managed
    }

    internal void PrepareReceive()
    {
    }
}
