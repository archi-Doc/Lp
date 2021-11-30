// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class NetTerminalClient : NetTerminal
{
    internal NetTerminalClient(Terminal terminal, NodeAddress nodeAddress)
        : base(terminal, nodeAddress)
    {// NodeAddress: Unmanaged
    }

    internal NetTerminalClient(Terminal terminal, NodeInformation nodeInformation)
        : base(terminal, nodeInformation, LP.Random.Crypto.NextULong())
    {// NodeInformation: Managed
    }

    public INetInterface<TSend> SendSingle<TSend>(TSend value)
        where TSend : IPacket
    {
        if (!value.AllowUnencrypted)
        {
            var result = this.ConnectAndEncrypt();
            if (result != NetInterfaceResult.Success)
            {
                return NetInterface<TSend, object>.CreateError(this, result);
            }
        }

        return this.SendPacket(value);
    }

    public INetInterface<TSend, TReceive> SendSingleAndReceive<TSend, TReceive>(TSend value)
        where TSend : IPacket
    {
        if (!value.AllowUnencrypted)
        {
            var result = this.ConnectAndEncrypt();
            if (result != NetInterfaceResult.Success)
            {
                return (INetInterface<TSend, TReceive>)NetInterface<TSend, object>.CreateError(this, result);
            }
        }

        return this.SendAndReceivePacket<TSend, TReceive>(value);
    }

    /*public INetInterface<TSend> Send<TSend>(TSend value)
    {
        if (value is IPacket packet && !packet.AllowUnencrypted)
        {
            if (!this.CheckManagedAndEncrypted())
            {
                return null!;
            }
        }

        return this.SendPacket(value);
    }*/

    public INetInterface<TSend, TReceive> SendAndReceive<TSend, TReceive>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {
        if (!value.AllowUnencrypted)
        {
            var result = this.ConnectAndEncrypt();
            if (result != NetInterfaceResult.Success)
            {
                return (INetInterface<TSend, TReceive>)NetInterface<TSend, object>.CreateError(this, result);
            }
        }

        var netInterface = this.SendAndReceivePacket<TSend, TReceive>(value);
        return netInterface;
    }

    public NetInterfaceResult ConnectAndEncrypt()
    {
        if (this.IsEncrypted)
        {// Encrypted
            return NetInterfaceResult.Success;
        }
        else if (this.NodeInformation == null)
        {// Unmanaged
            return NetInterfaceResult.NoNodeInformation;
        }

        var p = new PacketConnect(this.Terminal.NetStatus.GetMyNodeInformation());
        var netInterface = this.SendSingleAndReceive<PacketConnect, PacketConnectResponse>(p);
        if (netInterface.Receive(out var response) != NetInterfaceReceiveResult.Success)
        {// if (netInterface.WaitForSendCompletion() != NetInterfaceSendResult.Success)
            netInterface.Dispose();
            return NetInterfaceResult.NoSecureConnection;
        }

        return this.CreateEmbryo(p.Salt);
    }
}
