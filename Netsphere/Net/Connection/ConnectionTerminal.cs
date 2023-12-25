// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Net;
using Netsphere.Packet;
using Netsphere.Server;
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
        this.NetTerminal = netTerminal;
        this.AckBuffer = new(this);
        this.packetTerminal = this.NetTerminal.PacketTerminal;
        this.netStats = this.NetTerminal.NetStats;

        this.logger = this.NetTerminal.UnitLogger.GetLogger<ConnectionTerminal>();
    }

    public NetBase NetBase { get; }

    internal UnitLogger UnitLogger => this.NetTerminal.UnitLogger;

    internal NetTerminal NetTerminal { get; }

    internal AckBuffer AckBuffer { get; }

    internal FlowControl SharedFlowControl { get; } = new(NetConstants.SendCapacityPerRound);

    private readonly PacketTerminal packetTerminal;
    private readonly NetStats netStats;
    private readonly ILogger logger;

    private readonly ClientConnection.GoshujinClass clientConnections = new();
    private readonly ServerConnection.GoshujinClass serverConnections = new();

    private readonly FlowControl.GoshujinClass flowControls = new();

    public void Clean()
    {
        var systemCurrentMics = Mics.GetSystem();

        lock (this.clientConnections.SyncObject)
        {
            // Close unused client connections
            while (this.clientConnections.OpenListChain.First is { } clientConnection &&
                clientConnection.responseSystemMics + FromOpenToClosedMics < systemCurrentMics)
            {
                clientConnection.Logger.TryGet(LogLevel.Debug)?.Log($"{clientConnection.ConnectionIdText} Close unused");

                clientConnection.SendCloseFrame();
                this.CloseClientConnection(this.clientConnections, clientConnection);
                clientConnection.CloseTransmission();
            }

            // Dispose closed client connections
            while (this.clientConnections.ClosedListChain.First is { } connection &&
                connection.closedSystemMics + FromClosedToDisposalMics < systemCurrentMics)
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
                serverConnection.responseSystemMics + FromOpenToClosedMics + AdditionalServerMics < systemCurrentMics)
            {
                serverConnection.Logger.TryGet(LogLevel.Debug)?.Log($"{serverConnection.ConnectionIdText} Close unused");
                serverConnection.SendCloseFrame();
                this.CloseServerConnection(this.serverConnections, serverConnection);
                serverConnection.CloseTransmission();
            }

            // Dispose closed server connections
            while (this.serverConnections.ClosedListChain.First is { } connection &&
                connection.closedSystemMics + FromClosedToDisposalMics + AdditionalServerMics < systemCurrentMics)
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
            if (mode == Connection.ConnectMode.Shared)
            {// Attempt to share connections that have already been created and are open.
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
                    if ((connection.closedSystemMics + FromClosedToDisposalMics) > systemMics)
                    {// ConnectionStateCode
                        this.clientConnections.ClosedListChain.Remove(connection);
                        this.clientConnections.ClosedEndPointChain.Remove(connection);
                        connection.closedSystemMics = 0;

                        this.clientConnections.OpenListChain.AddLast(connection);
                        this.clientConnections.OpenEndPointChain.Add(endPoint, connection);
                        connection.responseSystemMics = Mics.GetSystem();
                        return connection;
                    }
                }
            }
        }

        // Create a new connection
        var packet = new PacketConnect(0, this.NetTerminal.NetBase.NodePublicKey);
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
            newConnection.responseSystemMics = Mics.GetSystem();
        }

        return newConnection;
    }

    internal ClientConnection? PrepareClientSide(NetEndPoint endPoint, NodePublicKey serverPublicKey, PacketConnect p, PacketConnectResponse p2)
    {
        // KeyMaterial
        var pair = new NodeKeyPair(this.NetTerminal.NetBase.NodePrivateKey, serverPublicKey);
        var material = pair.DeriveKeyMaterial();
        if (material is null)
        {
            return default;
        }

        this.CreateEmbryo(material, p, p2, out var connectionId, out var embryo);
        var connection = new ClientConnection(this.NetTerminal.PacketTerminal, this, connectionId, endPoint);
        connection.Initialize(p2.Agreement, embryo);

        return connection;
    }

    internal bool PrepareServerSide(NetEndPoint endPoint, PacketConnect p, PacketConnectResponse p2)
    {
        // KeyMaterial
        var pair = new NodeKeyPair(this.NetTerminal.NetBase.NodePrivateKey, p.ClientPublicKey);
        var material = pair.DeriveKeyMaterial();
        if (material is null)
        {
            return false;
        }

        this.CreateEmbryo(material, p, p2, out var connectionId, out var embryo);
        var connection = new ServerConnection(this.NetTerminal.PacketTerminal, this, connectionId, endPoint);
        connection.Initialize(p2.Agreement, embryo);

        lock (this.serverConnections.SyncObject)
        {// ConnectionStateCode
            connection.Goshujin = this.serverConnections;
            this.serverConnections.OpenListChain.AddLast(connection);
            this.serverConnections.OpenEndPointChain.Add(connection.EndPoint, connection);
            connection.responseSystemMics = Mics.GetSystem();
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
                    connection.Logger.TryGet(LogLevel.Debug)?.Log($"{connection.ConnectionIdText} Open -> Closed, SendCloseFrame {sendCloseFrame}");

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
                    connection.Logger.TryGet(LogLevel.Debug)?.Log($"{connection.ConnectionIdText} Open -> Closed, SendCloseFrame {sendCloseFrame}");

                    if (sendCloseFrame)
                    {
                        serverConnection.SendCloseFrame();
                    }

                    this.CloseServerConnection(g2, serverConnection);
                }
            }
        }
    }

    internal void CreateFlowControl(Connection connection)
    {
        lock (this.flowControls.SyncObject)
        {
            if (connection.flowControl is null)
            {
                connection.flowControl = new(connection);
                connection.flowControl.Goshujin = this.flowControls;
            }
        }
    }

    internal void ProcessSend(NetSender netSender)
    {
        var count = 0;
        lock (this.flowControls.SyncObject)
        {
            var current = this.flowControls.ListChain.First;
            while (current is not null)
            {
                var next = current.ListLink.Next;

                if (current.Connection is { } connection)
                {
                    if (connection.State == Connection.ConnectionState.Closed ||
                        connection.State == Connection.ConnectionState.Disposed)
                    {// Connection closed
                        current.Goshujin = null;
                        current.Clear();
                        current = next;
                        continue;
                    }
                }

                if (current.IsEmpty)
                {// Empty
                    if (current.MarkedForDeletion)
                    {// Delete
                        current.Goshujin = null;
                        current.Clear();
                    }
                    else
                    {// To prevent immediate deletion after creation, just set the deletion flag.
                        current.MarkedForDeletion = true;
                    }
                }
                else
                {// Not empty
                    current.MarkedForDeletion = false;
                    count++;
                    netSender.FlowControlQueue.Enqueue(current);
                }

                current = next;
            }
        }

        // Send
        this.SharedFlowControl.ProcessSend(netSender);
        while (netSender.FlowControlQueue.TryDequeue(out var x))
        {
            x.ProcessSend(netSender);
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

    internal void InvokeServer(TransmissionContext transmissionContext)
    {
        if (transmissionContext.DataKind == 0)
        {// Block (Responder)
            transmissionContext.SendAndForget(new PacketPingResponse(NetAddress.Alternative, "Alternativ"));
        }
        else if (transmissionContext.DataKind == 1)
        {// RPC
        }
    }

    private void CloseClientConnection(ClientConnection.GoshujinClass g, ClientConnection connection)
    {// lock (g.SyncObject)
        // ConnectionStateCode
        g.OpenListChain.Remove(connection);
        g.OpenEndPointChain.Remove(connection);
        connection.responseSystemMics = 0;

        g.ClosedEndPointChain.Add(connection.EndPoint, connection);
        g.ClosedListChain.AddLast(connection);
        connection.closedSystemMics = Mics.GetSystem();
    }

    private void CloseServerConnection(ServerConnection.GoshujinClass g, ServerConnection connection)
    {// lock (g.SyncObject)
        // ConnectionStateCode
        g.OpenListChain.Remove(connection);
        g.OpenEndPointChain.Remove(connection);
        connection.responseSystemMics = 0;

        g.ClosedEndPointChain.Add(connection.EndPoint, connection);
        g.ClosedListChain.AddLast(connection);
        connection.closedSystemMics = Mics.GetSystem();
    }
}
