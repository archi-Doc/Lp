// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Netsphere.Crypto;
using Netsphere.Net;
using Netsphere.Packet;
using Netsphere.Stats;

namespace Netsphere;

public class ConnectionTerminal
{// ConnectionStateCode: Open -> Closed -> Disposed
    private static readonly long FromOpenToClosedMics = Mics.FromSeconds(5);
    private static readonly long FromClosedToDisposalMics = Mics.FromSeconds(10);
    private static readonly long AdditionalServerMics = Mics.FromSeconds(1);

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

    private readonly object syncQueue = new();
    private readonly Queue<NetTransmission> sendQueue = new();
    private readonly Queue<NetTransmission> resendQueue = new();

    public void Clean()
    {
        var systemCurrentMics = Mics.GetSystem();

        lock (this.clientConnections.SyncObject)
        {
            // Close unused client connections
            while (this.clientConnections.OpenListChain.First is { } clientConnection &&
                clientConnection.ResponseSystemMics + FromOpenToClosedMics < systemCurrentMics)
            {
                // Console.WriteLine($"Closed: {clientConnection.ToString()}");
                this.CloseClientConnection(this.clientConnections, clientConnection);
            }

            // Dispose closed client connections
            while (this.clientConnections.ClosedListChain.First is { } connection &&
                connection.ClosedSystemMics + FromClosedToDisposalMics < systemCurrentMics)
            {
                // Console.WriteLine($"Disposed: {connection.ToString()}");
                connection.Goshujin = null;
                connection.DisposeActual();
            }
        }

        lock (this.serverConnections.SyncObject)
        {
            // Close unused server connections
            while (this.serverConnections.OpenListChain.First is { } serverConnection &&
                serverConnection.ResponseSystemMics + FromOpenToClosedMics + AdditionalServerMics < systemCurrentMics)
            {
                // Console.WriteLine($"Closed: {serverConnection.ToString()}");
                this.CloseServerConnection(this.serverConnections, serverConnection);
            }

            // Dispose closed server connections
            while (this.serverConnections.ClosedListChain.First is { } connection &&
                connection.ClosedSystemMics + FromClosedToDisposalMics + AdditionalServerMics < systemCurrentMics)
            {
                // Console.WriteLine($"Disposed: {connection.ToString()}");
                connection.Goshujin = null;
                connection.DisposeActual();
            }
        }
    }

    public async Task<ClientConnection?> TryConnect(NetNode node, Connection.ConnectMode mode = Connection.ConnectMode.ReuseClosed)
    {
        if (!this.netStats.TryCreateEndPoint(node, out var endPoint))
        {
            return null;
        }

        var systemMics = Mics.GetSystem();
        lock (this.clientConnections.SyncObject)
        {
            if (mode == Connection.ConnectMode.OnlyConnected)
            {// Attempt to reuse connections that have already been created and are open.
                if (this.clientConnections.OpenEndPointChain.TryGetValue(endPoint, out var connection))
                {
                    return connection;
                }

                return null;
            }

            if (mode == Connection.ConnectMode.ReuseClosed)
            {// Attempt to reuse connections that have already been closed and are awaiting disposal.
                if (this.clientConnections.ClosedEndPointChain.TryGetValue(endPoint, out var connection))
                {
                    if ((connection.ClosedSystemMics + FromClosedToDisposalMics) > systemMics)
                    {// ConnectionStateCode
                        this.clientConnections.ClosedListChain.Remove(connection);
                        this.clientConnections.ClosedEndPointChain.Remove(connection);
                        connection.ClosedSystemMics = 0;

                        this.clientConnections.OpenListChain.AddLast(connection);
                        this.clientConnections.OpenEndPointChain.Add(endPoint, connection);
                        connection.ResponseSystemMics = Mics.GetSystem();
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

        newConnection.AddRtt(t.RttMics);
        lock (this.clientConnections.SyncObject)
        {// ConnectionStateCode
            newConnection.Goshujin = this.clientConnections;
            this.clientConnections.OpenListChain.AddLast(newConnection);
            this.clientConnections.OpenEndPointChain.Add(newConnection.EndPoint, newConnection);
            newConnection.ResponseSystemMics = Mics.GetSystem();
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
        connection.Initialize(p2.Agreement, embryo);

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
        connection.Initialize(p2.Agreement, embryo);

        lock (this.serverConnections.SyncObject)
        {// ConnectionStateCode
            connection.Goshujin = this.serverConnections;
            this.serverConnections.OpenListChain.AddLast(connection);
            this.serverConnections.OpenEndPointChain.Add(connection.EndPoint, connection);
            connection.ResponseSystemMics = Mics.GetSystem();
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

    internal void CloseInternal(Connection connection, bool sendCloseFrame)
    {
        if (connection is ClientConnection clientConnection &&
            clientConnection.Goshujin is { } g)
        {
            lock (g.SyncObject)
            {
                if (connection.State == Connection.ConnectionState.Open)
                {// Open -> Close
                    if (sendCloseFrame)
                    {
                        clientConnection.SendCloseFrame();
                    }

                    this.CloseClientConnection(g, clientConnection);
                }
            }
        }
        else if (connection is ServerConnection serverConnection &&
            serverConnection.Goshujin is { } g2)
        {
            lock (g2.SyncObject)
            {
                if (connection.State == Connection.ConnectionState.Open)
                {// Open -> Close
                    if (sendCloseFrame)
                    {
                        serverConnection.SendCloseFrame();
                    }

                    this.CloseServerConnection(g2, serverConnection);
                }
            }
        }
    }

    internal void ProcessReceive(IPEndPoint endPoint, ushort packetUInt16, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {
        // PacketHeaderCode
        var connectionId = BitConverter.ToUInt64(toBeShared.Span.Slice(8)); // ConnectionId

        if (packetUInt16 < 384)
        {// Client -> Server
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
        {// Server -> Client (Response)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void RegisterSend(NetTransmission transmission)
    {
        lock (this.syncQueue)
        {
            this.sendQueue.Enqueue(transmission);
        }
    }

    internal void ProcessSend(NetSender netSender)
    {
        lock (this.syncQueue)
        {
            // Send queue
            while (netSender.SendCapacity >= netSender.SendCount + NetTransmission.BlockThreshold)
            {
                if (!this.sendQueue.TryDequeue(out var transmission))
                {// No send queue
                    return;
                }

                if (transmission.SendInternal(netSender, out _))
                {// Success
                    this.resendQueue.Enqueue(transmission);
                }
            }

            // Resend queue
            while (netSender.SendCapacity >= netSender.SendCount + NetTransmission.BlockThreshold)
            {
                if (!this.resendQueue.TryPeek(out var transmission))
                {// No resend queue
                    break;
                }

                var sentMics = transmission.GetLargestSentMics();
                if (netSender.CurrentSystemMics < sentMics + NetGene.ResendMics)
                {// Wait until ResendMics elapses.
                    break;
                }

                transmission = this.resendQueue.Dequeue();
                if (transmission.SendInternal(netSender, out var sentCount) &&
                    sentCount > 0)
                {// Resend
                    this.resendQueue.Enqueue(transmission);
                }

                /*if (!transmission.CheckResend(netSender))
                {
                    break;
                }*/
            }
        }
    }

    private void CloseClientConnection(ClientConnection.GoshujinClass g, ClientConnection connection)
    {// lock (g.SyncObject)
     // ConnectionStateCode
        g.OpenListChain.Remove(connection);
        g.OpenEndPointChain.Remove(connection);
        connection.ResponseSystemMics = 0;

        g.ClosedEndPointChain.Add(connection.EndPoint, connection);
        g.ClosedListChain.AddLast(connection);
        connection.ClosedSystemMics = Mics.GetSystem();
    }

    private void CloseServerConnection(ServerConnection.GoshujinClass g, ServerConnection connection)
    {// lock (g.SyncObject)
     // ConnectionStateCode
        g.OpenListChain.Remove(connection);
        g.OpenEndPointChain.Remove(connection);
        connection.ResponseSystemMics = 0;

        g.ClosedEndPointChain.Add(connection.EndPoint, connection);
        g.ClosedListChain.AddLast(connection);
        connection.ClosedSystemMics = Mics.GetSystem();
    }
}
