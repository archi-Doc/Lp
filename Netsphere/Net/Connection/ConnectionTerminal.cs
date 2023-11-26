// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Packet;
using Netsphere.Stats;

namespace Netsphere;

public class ConnectionTerminal
{// NetConnection: Open(OpenEndPointChain) ->
    public ConnectionTerminal(NetTerminal netTerminal)
    {
        this.NetBase = netTerminal.NetBase;
        this.netTerminal = netTerminal;
        this.packetTerminal = this.netTerminal.PacketTerminal;
        this.netStats = this.netTerminal.NetStats;
    }

    public NetBase NetBase { get; }

    private readonly NetTerminal netTerminal;
    private readonly PacketTerminal packetTerminal;
    private readonly NetStats netStats;

    private readonly ClientConnection.GoshujinClass clientConnections = new();
    private readonly ServerConnection.GoshujinClass serverConnections = new();

    public async Task<ClientConnection?> TryConnect(NetNode node, Connection.ConnectMode mode = Connection.ConnectMode.ReuseClosed)
    {
        if (!this.netStats.TryCreateEndPoint(node, out var endPoint))
        {
            return null;
        }

        var systemMics = Mics.GetSystem();
        lock (this.clientConnections.SyncObject)
        {
            if (mode == Connection.ConnectMode.ReuseOpen)
            {// Attempt to reuse connections that have already been created and are open.
                if (this.clientConnections.OpenEndPointChain.TryGetValue(endPoint, out var connection))
                {
                    return connection;
                }
            }

            if (mode == Connection.ConnectMode.ReuseOpen ||
                mode == Connection.ConnectMode.ReuseClosed)
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
        var packet = new PacketConnect(0, this.netTerminal.NetBase.NodePublicKey);
        var t = await this.packetTerminal.SendAndReceiveAsync<PacketConnect, PacketConnectResponse>(node.Address, packet).ConfigureAwait(false);
        if (t.Value is null)
        {
            return default;
        }

        var newConnection = this.PrepareClientSide(endPoint, node.PublicKey, packet, t.Value);
        if (newConnection is null)
        {
            return default;
        }

        lock (this.clientConnections.SyncObject)
        {
            newConnection.Goshujin = this.clientConnections;
            this.clientConnections.OpenEndPointChain.Add(newConnection.EndPoint, newConnection);
        }

        return newConnection;
    }

    internal ClientConnection? PrepareClientSide(NetEndPoint endPoint, NodePublicKey serverPublicKey, PacketConnect p, PacketConnectResponse p2)
    {
        // KeyMaterial
        var pair = new NodeKeyPair(this.netTerminal.NetBase.NodePrivateKey, serverPublicKey);
        var material = pair.DeriveKeyMaterial();
        if (material is null)
        {
            return default;
        }

        this.CreateEmbryo(material, p, p2, out var connectionId, out var embryo);
        var connection = new ClientConnection(this.netTerminal.PacketTerminal, this, connectionId, endPoint);
        connection.Initialize(embryo);

        return connection;
    }

    internal bool PrepareServerSide(NetEndPoint endPoint, PacketConnect p, PacketConnectResponse p2)
    {
        // KeyMaterial
        var pair = new NodeKeyPair(this.netTerminal.NetBase.NodePrivateKey, p.ClientPublicKey);
        var material = pair.DeriveKeyMaterial();
        if (material is null)
        {
            return false;
        }

        this.CreateEmbryo(material, p, p2, out var connectionId, out var embryo);
        var connection = new ServerConnection(this.netTerminal.PacketTerminal, this, connectionId, endPoint);
        connection.Initialize(embryo);

        lock (this.serverConnections.SyncObject)
        {
            connection.Goshujin = this.serverConnections;
            this.serverConnections.OpenEndPointChain.Add(connection.EndPoint, connection);
        }

        return true;
    }

    internal void CreateEmbryo(byte[] material, PacketConnect p, PacketConnectResponse p2, out ulong connectionId, out Embryo embryo)
    {// ClientSalt, ServerSalt, Material, ClientSalt2, ServerSalt2
        Span<byte> buffer = stackalloc byte[sizeof(ulong) + sizeof(ulong) + KeyHelper.PrivateKeyLength + sizeof(ulong) + sizeof(ulong)];
        var span = buffer;
        BitConverter.TryWriteBytes(span, p.ClientSalt);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, p2.ServerSalt);
        span = span.Slice(sizeof(ulong));
        material.AsSpan().CopyTo(span);
        span = span.Slice(KeyHelper.PrivateKeyLength);
        BitConverter.TryWriteBytes(span, p.ClientSalt2);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, p2.ServerSalt2);

        Span<byte> hash = stackalloc byte[64];
        Arc.Crypto.Sha3Helper.Get512_Span(buffer, hash);

        var salt = BitConverter.ToUInt64(hash);
        hash = hash.Slice(sizeof(ulong));

        connectionId = BitConverter.ToUInt64(hash);
        hash = hash.Slice(sizeof(ulong));

        var key = new byte[32];
        hash.Slice(0, 32).CopyTo(key);
        hash = hash.Slice(32);

        var iv = new byte[16];
        hash.CopyTo(iv);
        embryo = new(salt, key, iv);
    }

    internal void CloseInternal(Connection connection, bool dispose, bool sendCloseFrame)
    {
        if (connection is ClientConnection clientConnection &&
            clientConnection.Goshujin is { } g)
        {
            lock (g.SyncObject)
            {
                if (dispose)
                {// -> Dispose
                    clientConnection.Goshujin = null;
                    clientConnection.DisposeActual();
                }
                else
                {// -> Close
                    if (connection.State == Connection.ConnectionState.Open)
                    {// Open -> Close
                        if (sendCloseFrame)
                        {
                            clientConnection.SendCloseFrame();
                        }

                        g.OpenEndPointChain.Remove(clientConnection);
                        g.ClosedEndPointChain.Add(clientConnection.EndPoint, clientConnection);
                        clientConnection.ClosedSystemMics = Mics.GetSystem();
                    }
                }
            }
        }
        else if (connection is ServerConnection serverConnection &&
            serverConnection.Goshujin is { } g2)
        {
            lock (g2.SyncObject)
            {
                if (dispose)
                {// -> Dispose
                    serverConnection.Goshujin = null;
                    serverConnection.DisposeActual();
                }
                else
                {// -> Close
                    if (connection.State == Connection.ConnectionState.Open)
                    {// Open -> Close
                        if (sendCloseFrame)
                        {
                            serverConnection.SendCloseFrame();
                        }

                        g2.OpenEndPointChain.Remove(serverConnection);
                        g2.ClosedEndPointChain.Add(serverConnection.EndPoint, serverConnection);
                        serverConnection.ClosedSystemMics = Mics.GetSystem();
                    }
                }
            }
        }
    }

    internal void ProcessReceive(IPEndPoint endPoint, ushort packetUInt16, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {
        // PacketHeaderCode
        var connectionId = BitConverter.ToUInt64(toBeShared.Span.Slice(8)); // ConnectionId

        if (packetUInt16 < 384)
        {
            ServerConnection? connection = default;
            lock (this.serverConnections.SyncObject)
            {
                this.serverConnections.ConnectionIdChain.TryGetValue(connectionId, out connection);
            }

            if (connection is not null &&
                connection.EndPoint.EndPointEquals(endPoint))
            {
                connection.ProcessReceive(endPoint, toBeShared, currentSystemMics);
            }
        }
        else
        {// Response
            ClientConnection? connection = default;
            lock (this.clientConnections.SyncObject)
            {
                this.clientConnections.ConnectionIdChain.TryGetValue(connectionId, out connection);
            }

            if (connection is not null &&
                connection.EndPoint.EndPointEquals(endPoint))
            {
                connection.ProcessReceive(endPoint, toBeShared, currentSystemMics);
            }
        }
    }
}
