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

    public override NetInterfaceResult ConnectAndEncrypt()
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
