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
        return netInterface;
    }

    public INetInterface<TSend, TReceive> SendAndReceiveRaw<TSend, TReceive>(TSend value)
        where TSend : IRawPacket
    {
        var netInterface = this.SendAndReceivePacket<TSend, TReceive>(value);
        return netInterface;
    }

    public INetInterface<TSend, TReceive> SendAndReceive<TSend, TReceive>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IRawPacket
    {
        if (!this.CheckManagedAndEncrypted())
        {
            return null!;
        }

        var netInterface = this.SendAndReceivePacket<TSend, TReceive>(value);
        return netInterface;
    }

    protected bool CheckManagedAndEncrypted()
    {
        if (this.embryo != null)
        {// Encrypted
            return true;
        }
        else if (this.NodeInformation == null)
        {// Unmanaged
            return false;
        }

        var p = new RawPacketEncrypt(this.Terminal.NetStatus.GetMyNodeInformation());
        var netInterface = this.SendRaw<RawPacketEncrypt>(p);
        if (netInterface.WaitForSendCompletion() != NetInterfaceSendResult.Success)
        {
            netInterface.Dispose();
            return false;
        }

        return this.CreateEmbryo(p.Salt);
    }
}
