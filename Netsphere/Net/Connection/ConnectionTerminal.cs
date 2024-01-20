// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arc.Collections;
using Netsphere.Crypto;
using Netsphere.Net;
using Netsphere.Packet;
using Netsphere.Stats;

#pragma warning disable SA1214
#pragma warning disable SA1401 // Fields should be private

namespace Netsphere;

public class ConnectionTerminal
{// ConnectionStateCode: Open -> Closed -> Disposed
    private static readonly long AdditionalServerMics = Mics.FromSeconds(1);

    public ConnectionTerminal(NetTerminal netTerminal)
    {
        this.NetBase = netTerminal.NetBase;
        this.NetTerminal = netTerminal;
        this.AckBuffer = new(this);
        this.packetTerminal = this.NetTerminal.PacketTerminal;
        this.netStats = this.NetTerminal.NetStats;
        this.CongestionControlList.AddFirst(this.NoCongestionControl);

        this.logger = this.NetTerminal.UnitLogger.GetLogger<ConnectionTerminal>();
    }

    public NetBase NetBase { get; }

    internal UnitLogger UnitLogger => this.NetTerminal.UnitLogger;

    internal NetTerminal NetTerminal { get; }

    internal AckBuffer AckBuffer { get; }

    internal ICongestionControl NoCongestionControl { get; } = new NoCongestionControl();

    internal readonly object SyncSend = new();
    internal UnorderedLinkedList<Connection> SendList = new(); // lock (this.SyncSend)
    internal UnorderedLinkedList<Connection> CongestedList = new(); // lock (this.SyncSend)

    // lock (this.CongestionControlList)
    internal UnorderedLinkedList<ICongestionControl> CongestionControlList = new();
    private long lastCongestionControlMics;

    private readonly PacketTerminal packetTerminal;
    private readonly NetStats netStats;
    private readonly ILogger logger;

    private readonly ClientConnection.GoshujinClass clientConnections = new();
    private readonly ServerConnection.GoshujinClass serverConnections = new();

    public void Clean()
    {
        var systemCurrentMics = Mics.GetSystem();

        lock (this.clientConnections.SyncObject)
        {
            // Close unused client connections
            while (this.clientConnections.OpenListChain.First is { } clientConnection &&
                clientConnection.ResponseSystemMics + NetConstants.ConnectionOpenToClosedMics < systemCurrentMics)
            {
                clientConnection.Logger.TryGet(LogLevel.Debug)?.Log($"{clientConnection.ConnectionIdText} Close unused");

                clientConnection.SendCloseFrame();
                this.CloseClientConnection(this.clientConnections, clientConnection);
                clientConnection.CloseTransmission();
            }

            // Dispose closed client connections
            while (this.clientConnections.ClosedListChain.First is { } connection &&
                connection.ClosedSystemMics + NetConstants.ConnectionClosedToDisposalMics < systemCurrentMics)
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
                serverConnection.ResponseSystemMics + NetConstants.ConnectionOpenToClosedMics + AdditionalServerMics < systemCurrentMics)
            {
                serverConnection.Logger.TryGet(LogLevel.Debug)?.Log($"{serverConnection.ConnectionIdText} Close unused");
                serverConnection.SendCloseFrame();
                this.CloseServerConnection(this.serverConnections, serverConnection);
                serverConnection.CloseTransmission();
            }

            // Dispose closed server connections
            while (this.serverConnections.ClosedListChain.First is { } connection &&
                connection.ClosedSystemMics + NetConstants.ConnectionClosedToDisposalMics + AdditionalServerMics < systemCurrentMics)
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
                    if ((connection.ClosedSystemMics + NetConstants.ConnectionClosedToDisposalMics) > systemMics)
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
        var packet = new PacketConnect(0, this.NetTerminal.NodePublicKey);
        var t = await this.packetTerminal.SendAndReceiveAsync<PacketConnect, PacketConnectResponse>(node.Address, packet).ConfigureAwait(false);
        if (t.Value is null)
        {
            return default;
        }

        var newConnection = this.PrepareClientSide(node, endPoint, node.PublicKey, packet, t.Value);
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

    internal ClientConnection? PrepareClientSide(NetNode node, NetEndPoint endPoint, NodePublicKey serverPublicKey, PacketConnect p, PacketConnectResponse p2)
    {
        // KeyMaterial
        var pair = new NodeKeyPair(this.NetTerminal.NodePrivateKey, serverPublicKey);
        var material = pair.DeriveKeyMaterial();
        if (material is null)
        {
            return default;
        }

        this.CreateEmbryo(material, p, p2, out var connectionId, out var embryo);
        var connection = new ClientConnection(this.NetTerminal.PacketTerminal, this, connectionId, node, endPoint);
        connection.Initialize(p2.Agreement, embryo);

        return connection;
    }

    internal bool PrepareServerSide(NetEndPoint endPoint, PacketConnect p, PacketConnectResponse p2)
    {
        // KeyMaterial
        var pair = new NodeKeyPair(this.NetTerminal.NodePrivateKey, p.ClientPublicKey);
        var material = pair.DeriveKeyMaterial();
        if (material is null)
        {
            return false;
        }

        var node = new NetNode(in endPoint, p.ClientPublicKey);
        this.CreateEmbryo(material, p, p2, out var connectionId, out var embryo);
        var connection = new ServerConnection(this.NetTerminal.PacketTerminal, this, connectionId, node, endPoint);
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
                    connection.Logger.TryGet(LogLevel.Debug)?.Log($"{connection.ConnectionIdText} Open -> Closed, SendCloseFrame {sendCloseFrame}");

                    if (sendCloseFrame)
                    {
                        clientConnection.SendCloseFrame();
                    }

                    this.CloseClientConnection(g, clientConnection);
                    // connection.ResetFlowControl(); // -> ProcessSend()
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
                    // connection.ResetFlowControl(); // -> ProcessSend()
                }
            }
        }
    }

    internal void ProcessSend(NetSender netSender)
    {
        // CongestionControl
        lock (this.CongestionControlList)
        {
            var currentMics = Mics.FastSystem;
            var elapsedMics = this.lastCongestionControlMics == 0 ? 0 : currentMics - this.lastCongestionControlMics;
            this.lastCongestionControlMics = currentMics;
            var elapsedMilliseconds = elapsedMics * 0.001d;

            var congestionControlNode = this.CongestionControlList.First;
            while (congestionControlNode is not null)
            {
                if (!congestionControlNode.Value.Process(netSender, elapsedMics, elapsedMilliseconds))
                {
                    this.CongestionControlList.Remove(congestionControlNode);
                }

                congestionControlNode = congestionControlNode.Next;
            }
        }

        lock (this.SyncSend)
        {
            // Resend list
            /*while (netSender.ResendList.First is { } firstNode)
            {
                var gene = firstNode.Value;
                var connection = gene.SendTransmission.Connection;
                if (connection.SendNode is null)
                {
                    connection.SendNode = this.SendList.AddLast(connection);
                }

                if (gene.SendTransmission.SendNode is null)
                {
                    gene.SendTransmission.SendNode = connection.SendList.AddLast(gene.SendTransmission);
                }

                netSender.ResendList.Remove(firstNode);
            }*/

            // Resend queue
            while (netSender.ResendQueue.TryDequeue(out var gene))
            {
                var transmission = gene.SendTransmission;
                var connection = transmission.Connection;
                if (connection.SendNode is null)
                {
                    connection.SendNode = this.SendList.AddLast(connection);
                }

                if (transmission.SendNode is null)
                {
                    transmission.SendNode = connection.SendList.AddLast(transmission);
                }

                Console.WriteLine(gene.GeneSerial);
                transmission.SetResend(gene);
            }

            // CongestedList: Move to SendList when congestion is resolved.
            var currentNode = this.CongestedList.Last; // To maintain order in SendList, process from the last node.
            while (currentNode is not null)
            {
                var previousNode = currentNode.Previous;

                var connection = currentNode.Value;
                if (connection.CongestionControl is null ||
                    !connection.CongestionControl.IsCongested)
                {// No congestion control or not congested
                    this.CongestedList.Remove(currentNode);
                    this.SendList.AddFirst(currentNode);
                }

                currentNode = previousNode;
            }

            // SendList: For fairness, send packets one at a time
            while (this.SendList.First is { } node)
            {
                if (!netSender.CanSend)
                {
                    return;
                }

                var connection = node.Value;
                var result = connection.ProcessSingleSend(netSender);
                if (result == ProcessSendResult.Complete)
                {// Delete the node if there is no transmission to send.
                    this.SendList.Remove(node);
                    connection.SendNode = null;
                }
                else if (result == ProcessSendResult.Remaining)
                {// If there are remaining packets, move it to the end.
                    this.SendList.MoveToLast(node);
                }
                else
                {// If in a congested state, move it to the CongestedList.
                    this.SendList.Remove(node);
                    this.CongestedList.AddFirst(node);
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

                if (connection?.State == Connection.ConnectionState.Closed)
                {// Reopen
                    this.serverConnections.ClosedListChain.Remove(connection);
                    this.serverConnections.ClosedEndPointChain.Remove(connection);
                    connection.ClosedSystemMics = 0;

                    this.serverConnections.OpenListChain.AddLast(connection);
                    this.serverConnections.OpenEndPointChain.Add(connection.EndPoint, connection);
                    connection.ResponseSystemMics = Mics.GetSystem();
                }
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
