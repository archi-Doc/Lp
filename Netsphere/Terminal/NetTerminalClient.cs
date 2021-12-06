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

    public override NetInterfaceResult EncryptConnection()
    {
        if (this.IsEncrypted)
        {// Encrypted
            return NetInterfaceResult.Success;
        }
        else if (this.NodeInformation == null)
        {// Unmanaged
            return NetInterfaceResult.NoNodeInformation;
        }

        var p = new PacketEncrypt(this.Terminal.NetStatus.GetMyNodeInformation());
        var netInterface = this.SendPacketAndReceive<PacketEncrypt, PacketEncryptResponse>(p);
        if (netInterface.Receive(out var response) != NetInterfaceResult.Success)
        {
            netInterface.Dispose();
            return NetInterfaceResult.NoEncryptedConnection;
        }

        return this.CreateEmbryo(p.Salt);
    }

    public override async Task<NetInterfaceResult> EncryptConnectionAsync()
    {
        if (this.IsEncrypted)
        {// Encrypted
            return NetInterfaceResult.Success;
        }
        else if (this.NodeInformation == null)
        {// Unmanaged
            return NetInterfaceResult.NoNodeInformation;
        }

        var p = new PacketEncrypt(this.Terminal.NetStatus.GetMyNodeInformation());
        var response = await this.SendPacketAndReceiveAsync<PacketEncrypt, PacketEncryptResponse>(p).ConfigureAwait(false);
        if (response == null)
        {
            return NetInterfaceResult.NoEncryptedConnection;
        }

        return this.CreateEmbryo(p.Salt);
    }
}
