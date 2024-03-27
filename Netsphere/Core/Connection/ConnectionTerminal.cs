// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Collections;
using Netsphere.Core;
using Netsphere.Crypto;
using Netsphere.Packet;
using Netsphere.Stats;

#pragma warning disable SA1214
#pragma warning disable SA1401 // Fields should be private

namespace Netsphere;

public class ConnectionTerminal
{// ConnectionStateCode: Open -> Closed -> Disposed
    private static readonly long AdditionalServerMics = Mics.FromSeconds(1);

    public ConnectionTerminal(IServiceProvider serviceProvider, NetTerminal netTerminal)
    {
        this.ServiceProvider = serviceProvider;
        this.NetBase = netTerminal.NetBase;
        this.NetTerminal = netTerminal;
        this.AckQueue = new(this);
        this.packetTerminal = this.NetTerminal.PacketTerminal;
        this.netStats = this.NetTerminal.NetStats;
        this.CongestionControlList.AddFirst(this.NoCongestionControl);

        this.logger = this.NetTerminal.UnitLogger.GetLogger<ConnectionTerminal>();
    }

    #region FieldAndProperty

    public NetBase NetBase { get; }

    internal IServiceProvider ServiceProvider { get; }

    internal UnitLogger UnitLogger => this.NetTerminal.UnitLogger;

    internal NetTerminal NetTerminal { get; }

    internal AckBuffer AckQueue { get; }

    internal ICongestionControl NoCongestionControl { get; } = new NoCongestionControl();

    internal uint ReceiveTransmissionGap { get; private set; }

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

    #endregion

    public void Clean()
    {
        var systemCurrentMics = Mics.GetSystem();

        (UnorderedMap<NetEndPoint, ClientConnection>.Node[] Nodes, int Max) client;
        Queue<ClientConnection> clientToChange = new();
        lock (this.clientConnections.SyncObject)
        {
            client = this.clientConnections.DestinationEndPointChain.UnsafeGetNodes();
        }

        for (var i = 0; i < client.Max; i++)
        {
            if (client.Nodes[i].Value is { } clientConnection)
            {
                Debug.Assert(!clientConnection.IsDisposed);
                if (clientConnection.IsOpen)
                {
                    if (clientConnection.LastEventMics + clientConnection.Agreement.MinimumConnectionRetentionMics < systemCurrentMics)
                    {// Open -> Closed
                        clientConnection.Logger.TryGet(LogLevel.Debug)?.Log($"{clientConnection.ConnectionIdText} Close (unused)");
                        clientToChange.Enqueue(clientConnection);

                        clientConnection.SendCloseFrame();
                    }
                }
                else if (clientConnection.IsClosed)
                {// Closed -> Dispose
                    if (clientConnection.LastEventMics + NetConstants.ConnectionClosedToDisposalMics < systemCurrentMics)
                    {
                        clientConnection.Logger.TryGet(LogLevel.Debug)?.Log($"{clientConnection.ConnectionIdText} Disposed");
                        clientToChange.Enqueue(clientConnection);

                        clientConnection.CloseAllTransmission();
                    }
                }

                clientConnection.CleanTransmission();
            }
        }

        lock (this.clientConnections.SyncObject)
        {
            while (clientToChange.TryDequeue(out var clientConnection))
            {
                if (clientConnection.IsOpen)
                {// Open -> Closed
                    clientConnection.ChangeStateInternal(Connection.State.Closed);
                }
                else if (clientConnection.IsClosed)
                {// Closed -> Dispose
                    clientConnection.ChangeStateInternal(Connection.State.Disposed);
                    clientConnection.Goshujin = null;
                }
            }
        }

        (UnorderedMap<NetEndPoint, ServerConnection>.Node[] Nodes, int Max) server;
        Queue<ServerConnection> serverToChange = new();
        lock (this.serverConnections.SyncObject)
        {
            server = this.serverConnections.DestinationEndPointChain.UnsafeGetNodes();
        }

        for (var i = 0; i < server.Max; i++)
        {
            if (server.Nodes[i].Value is { } serverConnection)
            {
                Debug.Assert(!serverConnection.IsDisposed);
                if (serverConnection.IsOpen)
                {
                    if (serverConnection.LastEventMics + serverConnection.Agreement.MinimumConnectionRetentionMics < systemCurrentMics)
                    {// Open -> Closed
                        serverConnection.Logger.TryGet(LogLevel.Debug)?.Log($"{serverConnection.ConnectionIdText} Close (unused)");
                        serverToChange.Enqueue(serverConnection);

                        serverConnection.SendCloseFrame();
                    }
                }
                else if (serverConnection.IsClosed)
                {// Closed -> Dispose
                    if (serverConnection.LastEventMics + NetConstants.ConnectionClosedToDisposalMics + AdditionalServerMics < systemCurrentMics)
                    {
                        serverConnection.Logger.TryGet(LogLevel.Debug)?.Log($"{serverConnection.ConnectionIdText} Disposed");
                        serverToChange.Enqueue(serverConnection);

                        serverConnection.CloseAllTransmission();
                    }
                }

                serverConnection.CleanTransmission();
            }
        }

        lock (this.serverConnections.SyncObject)
        {
            while (serverToChange.TryDequeue(out var serverConnection))
            {
                if (serverConnection.IsOpen)
                {// Open -> Closed
                    serverConnection.ChangeStateInternal(Connection.State.Closed);
                }
                else if (serverConnection.IsClosed)
                {// Closed -> Dispose
                    serverConnection.ChangeStateInternal(Connection.State.Disposed);
                    serverConnection.Goshujin = null;
                }
            }
        }
    }

    public async Task<ClientConnection?> Connect(NetNode node, Connection.ConnectMode mode = Connection.ConnectMode.ReuseIfAvailable)
    {
        if (!this.NetTerminal.IsActive)
        {
            return null;
        }

        if (!this.netStats.TryCreateEndPoint(node, out var endPoint))
        {
            return null;
        }

        lock (this.clientConnections.SyncObject)
        {
            if (mode == Connection.ConnectMode.ReuseIfAvailable ||
                mode == Connection.ConnectMode.ReuseOnly)
            {// Attempts to reuse a connection that has already been connected or disconnected (but not yet disposed).
                if (this.clientConnections.DestinationEndPointChain.TryGetValue(endPoint, out var connection))
                {
                    Debug.Assert(!connection.IsDisposed);
                    connection.IncrementOpenCount();
                    connection.ChangeStateInternal(Connection.State.Open);
                    return connection;
                }

                if (mode == Connection.ConnectMode.ReuseOnly)
                {
                    return default;
                }
            }
        }

        // Create a new connection
        var packet = new ConnectPacket(0, this.NetTerminal.NodePublicKey, node.PublicKey.GetHashCode());
        var t = await this.packetTerminal.SendAndReceive<ConnectPacket, ConnectPacketResponse>(node.Address, packet).ConfigureAwait(false);
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
            newConnection.IncrementOpenCount();
            newConnection.Goshujin = this.clientConnections;
        }

        return newConnection;
    }

    internal ClientConnection PrepareBidirectionalConnection(ServerConnection serverConnection)
    {
        lock (this.clientConnections.SyncObject)
        {
            if (this.clientConnections.ConnectionIdChain.TryGetValue(serverConnection.ConnectionId, out var connection))
            {
                connection.IncrementOpenCount();
                connection.ChangeStateInternal(Connection.State.Open);
            }
            else
            {
                connection = new ClientConnection(serverConnection);
                connection.Goshujin = this.clientConnections;
            }

            serverConnection.BidirectionalConnection = connection;
            return connection;
        }
    }

    internal ServerConnection PrepareBidirectionalConnection(ClientConnection clientConnection)
    {
        lock (this.serverConnections.SyncObject)
        {
            if (this.serverConnections.ConnectionIdChain.TryGetValue(clientConnection.ConnectionId, out var connection))
            {
                connection.ChangeStateInternal(Connection.State.Open);
            }
            else
            {
                connection = new ServerConnection(clientConnection);
                connection.Goshujin = this.serverConnections;
            }

            clientConnection.BidirectionalConnection = connection;
            return connection;
        }
    }

    internal ClientConnection? PrepareClientSide(NetNode node, NetEndPoint endPoint, NodePublicKey serverPublicKey, ConnectPacket p, ConnectPacketResponse p2)
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

    internal bool PrepareServerSide(NetEndPoint endPoint, ConnectPacket p, ConnectPacketResponse p2)
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
        }

        return true;
    }

    internal void CreateEmbryo(byte[] material, ConnectPacket p, ConnectPacketResponse p2, out ulong connectionId, out Embryo embryo)
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
        connection.CloseSendTransmission();

        if (connection is ClientConnection clientConnection &&
            clientConnection.Goshujin is { } g)
        {
            ServerConnection? bidirectionalConnection;
            lock (g.SyncObject)
            {
                clientConnection.ResetOpenCountl();
                if (connection.CurrentState == Connection.State.Open)
                {// Open -> Close
                    connection.Logger.TryGet(LogLevel.Debug)?.Log($"{connection.ConnectionIdText} Open -> Closed, SendCloseFrame {sendCloseFrame}");

                    if (sendCloseFrame)
                    {
                        clientConnection.SendCloseFrame();
                    }

                    clientConnection.ChangeStateInternal(Connection.State.Closed);
                }

                bidirectionalConnection = clientConnection.BidirectionalConnection;
                if (bidirectionalConnection is not null)
                {
                    clientConnection.BidirectionalConnection = default;
                    bidirectionalConnection.BidirectionalConnection = default;
                }
            }

            if (bidirectionalConnection is not null)
            {
                this.CloseInternal(bidirectionalConnection, sendCloseFrame);
            }
        }
        else if (connection is ServerConnection serverConnection &&
            serverConnection.Goshujin is { } g2)
        {
            ClientConnection? bidirectionalConnection;
            lock (g2.SyncObject)
            {
                if (connection.CurrentState == Connection.State.Open)
                {// Open -> Close
                    connection.Logger.TryGet(LogLevel.Debug)?.Log($"{connection.ConnectionIdText} Open -> Closed, SendCloseFrame {sendCloseFrame}");

                    if (sendCloseFrame)
                    {
                        serverConnection.SendCloseFrame();
                    }

                    serverConnection.ChangeStateInternal(Connection.State.Closed);
                }

                bidirectionalConnection = serverConnection.BidirectionalConnection;
                if (bidirectionalConnection is not null)
                {
                    serverConnection.BidirectionalConnection = default;
                    bidirectionalConnection.BidirectionalConnection = default;
                }
            }

            if (bidirectionalConnection is not null)
            {
                this.CloseInternal(bidirectionalConnection, sendCloseFrame);
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
        if (NetConstants.LogLowLevelNet)
        {
            // this.logger.TryGet(LogLevel.Debug)?.Log($"Receive actual");
        }

        // PacketHeaderCode
        var connectionId = BitConverter.ToUInt64(toBeShared.Span.Slice(8)); // ConnectionId
        if (packetUInt16 < 384)
        {// Client -> Server
            ServerConnection? connection = default;
            lock (this.serverConnections.SyncObject)
            {
                this.serverConnections.ConnectionIdChain.TryGetValue(connectionId, out connection);

                if (connection?.CurrentState == Connection.State.Closed)
                {// Reopen (Closed -> Open)
                    connection.ChangeStateInternal(Connection.State.Open);
                }
            }

            if (connection is not null &&
                connection.DestinationEndPoint.EndPointEquals(endPoint))
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
                connection.DestinationEndPoint.EndPointEquals(endPoint))
            {
                connection.ProcessReceive(endPoint, toBeShared, currentSystemMics);
            }
        }
    }

    internal void SetReceiveTransmissionGapForTest(uint gap)
    {
        this.ReceiveTransmissionGap = gap;
    }

    internal async Task Terminate(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ClientConnection[] clients;
            lock (this.clientConnections.SyncObject)
            {
                clients = this.clientConnections.ToArray();
            }

            foreach (var x in clients)
            {
                x.TerminateInternal();
            }

            lock (this.clientConnections.SyncObject)
            {
                foreach (var x in clients)
                {
                    if (x.IsEmpty)
                    {
                        if (x.IsOpen)
                        {
                            x.ChangeStateInternal(Connection.State.Closed);
                        }

                        if (x.IsClosed)
                        {
                            x.ChangeStateInternal(Connection.State.Disposed);
                        }

                        x.Goshujin = null;
                    }
                }
            }

            ServerConnection[] servers;
            lock (this.serverConnections.SyncObject)
            {
                servers = this.serverConnections.ToArray();
            }

            foreach (var x in servers)
            {
                x.TerminateInternal();
            }

            lock (this.serverConnections.SyncObject)
            {
                foreach (var x in servers)
                {
                    if (x.IsEmpty)
                    {
                        if (x.IsOpen)
                        {
                            x.ChangeStateInternal(Connection.State.Closed);
                        }

                        if (x.IsClosed)
                        {
                            x.ChangeStateInternal(Connection.State.Disposed);
                        }

                        x.Goshujin = null;
                    }
                }
            }

            if (this.clientConnections.Count == 0 &&
                this.serverConnections.Count == 0)
            {
                return;
            }
            else
            {
                try
                {
                    await Task.Delay(NetConstants.TerminateTerminalDelayMilliseconds, cancellationToken);
                }
                catch
                {
                    return;
                }
            }
        }
    }
}
