// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

public class NetTerminalClient : NetTerminal
{
    internal NetTerminalClient(Terminal terminal, NodeAddress nodeAddress)
        : base(terminal, nodeAddress)
    {// NodeAddress: Unmanaged
    }

    internal NetTerminalClient(Terminal terminal, NodeInformation nodeInformation)
        : base(terminal, nodeInformation, Random.Crypto.NextULong())
    {// NodeInformation: Managed
    }

    public INetInterface<TSend> SendRaw<TSend>(TSend value)
        where TSend : IRawPacket
    {
        var netInterface = this.SendPacket(value);
        lock (this.SyncObject)
        {
            this.netInterfaces.Add(netInterface);
        }

        return netInterface;
    }

    public INetInterface<TSend, TReceive> SendAndReceiveRaw<TSend, TReceive>(TSend value)
        where TSend : IRawPacket
    {
        var netInterface = this.SendAndReceivePacket<TSend, TReceive>(value);
        lock (this.SyncObject)
        {
            this.netInterfaces.Add(netInterface);
        }

        return netInterface;
    }
}
