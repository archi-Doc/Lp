// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Packet;
using Netsphere.Stats;

namespace Netsphere;

internal class NetConnectionTerminal
{// NetConnection: Open(OpenEndPointChain) ->
    public NetConnectionTerminal(NetTerminal netTerminal)
    {
        this.netTerminal = netTerminal;
        this.packetTerminal = this.netTerminal.PacketTerminal;
        this.netStats = this.netTerminal.NetStats;
    }

    private readonly NetTerminal netTerminal;
    private readonly PacketTerminal packetTerminal;
    private readonly NetStats netStats;

    private readonly ClientConnection.GoshujinClass clientConnections = new();
    private readonly ServerConnection.GoshujinClass serverConnections = new();

    public async Task<ClientConnection?> TryConnect(NetNode node, NetConnection.ConnectMode mode = NetConnection.ConnectMode.ReuseClosed)
    {
        if (!this.netStats.TryCreateEndPoint(node, out var endPoint))
        {
            return null;
        }

        var systemMics = Mics.GetSystem();
        lock (this.clientConnections.SyncObject)
        {
            if (mode == NetConnection.ConnectMode.ReuseOpen)
            {// Attempt to reuse connections that have already been created and are open.
                if (this.clientConnections.OpenEndPointChain.TryGetValue(endPoint, out var connection))
                {
                    return connection;
                }
            }

            if (mode == NetConnection.ConnectMode.ReuseOpen ||
                mode == NetConnection.ConnectMode.ReuseClosed)
            {// Attempt to reuse connections that have already been closed and are awaiting disposal.
                if (this.clientConnections.ClosedEndPointChain.TryGetValue(endPoint, out var connection))
                {
                    if ((connection.ClosedSystemMics + Mics.FromMinutes(1)) > systemMics)
                    {
                        this.clientConnections.ClosedEndPointChain.Remove(connection);
                        connection.ClosedSystemMics = 0;
                        this.clientConnections.OpenEndPointChain.Add(endPoint, connection);
                        return connection;
                    }
                }
            }
        }

        // Create a new connection
        var packet = new PacketConnect(this.netTerminal.NetBase.NodePublicKey);
        var t = await this.packetTerminal.SendAndReceiveAsync<PacketConnect, PacketConnectResponse>(node.Address, packet).ConfigureAwait(false);
        if (t.Value is null)
        {
            return default;
        }

        return this.PrepareClientSide(node.PublicKey, packet, t.Value);
    }

    internal ClientConnection? PrepareClientSide(NodePublicKey serverPublicKey, PacketConnect p, PacketConnectResponse p2)
    {
        // KeyMaterial
        var pair = new NodeKeyPair(this.netTerminal.NetBase.NodePrivateKey, serverPublicKey);
        var material = pair.DeriveKeyMaterial();
        if (material is null)
        {
            return default;
        }
    }

    internal bool PrepareServerSide(PacketConnect p, PacketConnectResponse p2)
    {
        // KeyMaterial
        var pair = new NodeKeyPair(this.netTerminal.NetBase.NodePrivateKey, p.ClientPublicKey);
        var material = pair.DeriveKeyMaterial();
        if (material is null)
        {
            return false;
        }

        var connection = new ServerConnection()

        return true;
    }
}
