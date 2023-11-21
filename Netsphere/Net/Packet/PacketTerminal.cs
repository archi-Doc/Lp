// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

public sealed class PacketTerminal
{
    public PacketTerminal(NetTerminal netTerminal)
    {
        this.netTerminal = netTerminal;
    }

    private readonly NetTerminal netTerminal;
}
