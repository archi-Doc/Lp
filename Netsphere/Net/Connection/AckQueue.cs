// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Netsphere.Packet;
using static Arc.Unit.ByteArrayPool;

namespace Netsphere.Net;

internal partial class AckQueue
{
    public AckQueue(ConnectionTerminal connectionTerminal)
    {
        this.connectionTerminal = connectionTerminal;
        this.logger = connectionTerminal.UnitLogger.GetLogger<AckQueue>();
    }

    #region FieldAndProperty

    private readonly ConnectionTerminal connectionTerminal;
    private readonly ILogger logger;

    private readonly object syncObject = new();
    private readonly Queue<Connection> connectionQueue = new();
    private readonly Queue<Queue<uint>> freeAckRama = new();
    private readonly Queue<Queue<ReceiveTransmission>> freeAckBlock = new();
    private readonly Queue<Queue<int>> freeAckGene = new();

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AckRama(Connection connection, uint transmissionId)
    {
#if LOG_LOWLEVEL_NET
        this.logger.TryGet(LogLevel.Debug)?.Log($"AckRama {this.connectionTerminal.NetTerminal.NetTerminalString} to {connection.EndPoint.ToString()} {transmissionId}");
#endif

        lock (this.syncObject)
        {
            var ackRama = connection.AckRama;
            if (ackRama is null)
            {
                this.freeAckRama.TryDequeue(out ackRama); // Reuse the queued queue.
                ackRama ??= new();
                this.freeAckBlock.TryDequeue(out var ackBlock); // Reuse the queued queue.
                ackBlock ??= new();
                this.connectionQueue.Enqueue(connection);

                connection.AckMics = Mics.FastSystem + NetConstants.AckDelayMics;
                connection.AckRama = ackRama;
                connection.AckBlock = ackBlock;
            }

            ackRama.Enqueue(transmissionId);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AckBlock(Connection connection, ReceiveTransmission receiveTransmission, int geneSerial)
    {
#if LOG_LOWLEVEL_NET
        this.logger.TryGet(LogLevel.Debug)?.Log($"AckBlock {this.connectionTerminal.NetTerminal.NetTerminalString} to {connection.EndPoint.ToString()} {receiveTransmission.TransmissionId}-{geneSerial}");
#endif

        lock (this.syncObject)
        {
            var ackBlock = connection.AckBlock;
            if (ackBlock is null)
            {
                this.freeAckRama.TryDequeue(out var ackRama); // Reuse the queued queue.
                ackRama ??= new();
                this.freeAckBlock.TryDequeue(out ackBlock); // Reuse the queued queue.
                ackBlock ??= new();

                connection.AckMics = Mics.FastSystem + NetConstants.AckDelayMics;
                connection.AckRama = ackRama;
                connection.AckBlock = ackBlock;
                this.connectionQueue.Enqueue(connection);
            }

            var ackGene = receiveTransmission.AckGene;
            if (ackGene is null)
            {
                this.freeAckGene.TryDequeue(out ackGene); // Reuse the queued queue.
                ackGene ??= new();

                receiveTransmission.AckGene = ackGene;
                ackBlock.Enqueue(receiveTransmission);
            }

            ackGene.Enqueue(geneSerial);
        }
    }

    public void ProcessSend(NetSender netSender)
    {
        Connection? connection = default;
        Queue<uint>? ackRama = default;
        Queue<ReceiveTransmission>? ackBlock = default;

        while (true)
        {
            lock (this.syncObject)
            {
                if (ackRama is not null)
                {
                    this.freeAckRama.Enqueue(ackRama);
                    ackRama = default;
                }

                if (ackBlock is not null)
                {
                    this.freeAckBlock.Enqueue(ackBlock);
                    ackBlock = default;
                }

                if (!netSender.CanSend)
                {
                    return;
                }

                this.connectionQueue.TryPeek(out connection);
                if (connection is not null && Mics.FastSystem > connection.AckMics)
                {
                    this.connectionQueue.Dequeue();
                    ackRama = connection.AckRama;
                    ackBlock = connection.AckBlock;
                    connection.AckMics = 0;
                    connection.AckRama = default;
                    connection.AckBlock = default;
                }
            }

            // To shorten the acquisition time of the exclusive lock, temporarily release the lock.

            if (connection is null || ackRama is null || ackBlock is null)
            {
                break;
            }

            this.ProcessAck(netSender, connection, ackRama, ackBlock);
        }
    }

    private void ProcessAck(NetSender netSender, Connection connection, Queue<uint> ackRama, Queue<ReceiveTransmission> ackBlock)
    {
        const int maxLength = PacketHeader.MaxFrameLength - 2;
        ushort numberOfRama = 0;
        ushort numberOfBlock = 0;

        Owner? owner = default;
        Span<byte> span = default;

        while (ackRama.TryDequeue(out var transmissionId))
        {
            if (owner is not null && span.Length < AckFrame.Margin)
            {// Send the packet when the remaining length falls below the margin.
                Send(span.Length);
            }

            if (owner is null)
            {
                owner = PacketPool.Rent();
                span = owner.ByteArray.AsSpan(PacketHeader.Length + 6, maxLength); // PacketHeader, FrameType, NumberOfRama, NumberOfBlock
            }

            numberOfRama++;
            BitConverter.TryWriteBytes(span, transmissionId);
            span = span.Slice(sizeof(uint));
        }

        while (ackBlock.TryDequeue(out var transmission))
        {
            var transmissionId = transmission.TransmissionId;
            int successiveReceivedPosition = 0;
            int receiveCapacity = 0;
            int numberOfPairs = 0;
        }

        if (owner is not null && span.Length > 0)
        {// Send the packet if not empty.
            Send(span.Length);
        }

        owner?.Return(); // Return the rent buffer.
        // ackRama and ackBlock will be returned later for reuse.

        void Send(int spanLength)
        {
            connection.CreateAckPacket(owner, numberOfRama, numberOfBlock, spanLength, out var packetLength);
            netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, owner.ToMemoryOwner(0, packetLength));
            // owner = owner.Return(); // Moved
            owner = default;

            numberOfRama = 0;
            numberOfBlock = 0;
        }
    }

    private void ProcessAckOb(NetSender netSender, Connection connection, Queue<uint> ackRama, Queue<ReceiveTransmission> ackBlock)
    {// AckFrameCode
        ByteArrayPool.Owner? owner = default;
        var position = 0;
        var transmissionPosition = 0;
        const int maxLength = NetControl.MaxPacketLength - 16; // remainig = maxLength - position;
        uint previousTransmissionId = 0;
        ushort numberOfPairs = 0;
        int startGene = -1;
        int endGene = -1;

        while (ackQueue.TryDequeue(out var ack))
        {
            if ((maxLength - position) < AckFrame.Margin)
            {// Send the packet due to the size approaching the limit.
                SendPacket();
            }

            if (owner is null)
            {
                owner = PacketPool.Rent();
                transmissionPosition = PacketHeader.Length + AckFrame.Length;
                position = transmissionPosition + 6;
            }

            var transmissionId = (uint)(ack >> 32);
            var geneSerial = (int)ack;

            // this.logger.TryGet(LogLevel.Debug)?.Log($"ProcessAck: {transmissionId}, {geneSerial}");

            if (previousTransmissionId == 0)
            {// Initial transmission id
                previousTransmissionId = transmissionId;
                startGene = geneSerial;
                endGene = geneSerial + 1;
            }
            else if (transmissionId == previousTransmissionId)
            {// Same transmission id
                if (startGene == -1)
                {// Initial gene
                    startGene = geneSerial;
                    endGene = geneSerial + 1;
                }
                else if (endGene == geneSerial)
                {// Serial genes
                    endGene = geneSerial + 1;
                }
                else
                {// Not serial gene
                    var span = owner.ByteArray.AsSpan(position);
                    BitConverter.TryWriteBytes(span, startGene);
                    span = span.Slice(sizeof(int));
                    BitConverter.TryWriteBytes(span, endGene);
                    position += 8;
                    numberOfPairs++;

                    startGene = geneSerial;
                    endGene = geneSerial + 1;
                }
            }
            else
            {// Different transmission id
                // this.logger.TryGet(LogLevel.Debug)?.Log($"SendingAck: {previousTransmissionId}, {startGene} - {endGene}");

                var span = owner.ByteArray.AsSpan(position);
                BitConverter.TryWriteBytes(span, startGene);
                span = span.Slice(sizeof(int));
                BitConverter.TryWriteBytes(span, endGene);
                position += 8;
                numberOfPairs++;

                span = owner.ByteArray.AsSpan(transmissionPosition);
                BitConverter.TryWriteBytes(span, previousTransmissionId);
                span = span.Slice(sizeof(uint));
                BitConverter.TryWriteBytes(span, numberOfPairs);

                previousTransmissionId = transmissionId;
                numberOfPairs = 0;
                transmissionPosition = position;
                position += 6;

                startGene = geneSerial;
                endGene = geneSerial + 1;
            }
        }

        SendPacket();

        void SendPacket()
        {
            if (owner is not null)
            {
                // this.logger.TryGet(LogLevel.Debug)?.Log($"SendingAck: {previousTransmissionId}, {startGene} - {endGene}");

                if (previousTransmissionId != 0)
                {
                    var span = owner.ByteArray.AsSpan(position);
                    BitConverter.TryWriteBytes(span, startGene);
                    span = span.Slice(sizeof(int));
                    BitConverter.TryWriteBytes(span, endGene);
                    position += 8;
                    numberOfPairs++;

                    span = owner.ByteArray.AsSpan(transmissionPosition);
                    BitConverter.TryWriteBytes(span, previousTransmissionId);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, numberOfPairs);

                    previousTransmissionId = 0;
                    numberOfPairs = 0;
                    transmissionPosition = 0;
                    startGene = -1;
                    endGene = -1;
                }

                connection.CreateAckPacket(owner, position - PacketHeader.Length, out var packetLength);
                netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, owner.ToMemoryOwner(0, packetLength));
                // owner = owner.Return(); // Moved
            }
        }
    }

    private void ProcessAck(NetSender netSender, Connection connection, Queue<ulong> ackQueue)
    {// AckFrameCode
        ByteArrayPool.Owner? owner = default;
        var position = 0;
        var transmissionPosition = 0;
        const int maxLength = NetControl.MaxPacketLength - 16; // remainig = maxLength - position;
        uint previousTransmissionId = 0;
        ushort numberOfPairs = 0;
        int startGene = -1;
        int endGene = -1;

        while (ackQueue.TryDequeue(out var ack))
        {
            if ((maxLength - position) < AckFrame.Margin)
            {// Send the packet due to the size approaching the limit.
                SendPacket();
            }

            if (owner is null)
            {
                owner = PacketPool.Rent();
                transmissionPosition = PacketHeader.Length + AckFrame.Length;
                position = transmissionPosition + 6;
            }

            var transmissionId = (uint)(ack >> 32);
            var geneSerial = (int)ack;

            // this.logger.TryGet(LogLevel.Debug)?.Log($"ProcessAck: {transmissionId}, {geneSerial}");

            if (previousTransmissionId == 0)
            {// Initial transmission id
                previousTransmissionId = transmissionId;
                startGene = geneSerial;
                endGene = geneSerial + 1;
            }
            else if (transmissionId == previousTransmissionId)
            {// Same transmission id
                if (startGene == -1)
                {// Initial gene
                    startGene = geneSerial;
                    endGene = geneSerial + 1;
                }
                else if (endGene == geneSerial)
                {// Serial genes
                    endGene = geneSerial + 1;
                }
                else
                {// Not serial gene
                    var span = owner.ByteArray.AsSpan(position);
                    BitConverter.TryWriteBytes(span, startGene);
                    span = span.Slice(sizeof(int));
                    BitConverter.TryWriteBytes(span, endGene);
                    position += 8;
                    numberOfPairs++;

                    startGene = geneSerial;
                    endGene = geneSerial + 1;
                }
            }
            else
            {// Different transmission id
                // this.logger.TryGet(LogLevel.Debug)?.Log($"SendingAck: {previousTransmissionId}, {startGene} - {endGene}");

                var span = owner.ByteArray.AsSpan(position);
                BitConverter.TryWriteBytes(span, startGene);
                span = span.Slice(sizeof(int));
                BitConverter.TryWriteBytes(span, endGene);
                position += 8;
                numberOfPairs++;

                span = owner.ByteArray.AsSpan(transmissionPosition);
                BitConverter.TryWriteBytes(span, previousTransmissionId);
                span = span.Slice(sizeof(uint));
                BitConverter.TryWriteBytes(span, numberOfPairs);

                previousTransmissionId = transmissionId;
                numberOfPairs = 0;
                transmissionPosition = position;
                position += 6;

                startGene = geneSerial;
                endGene = geneSerial + 1;
            }
        }

        SendPacket();

        void SendPacket()
        {
            if (owner is not null)
            {
                // this.logger.TryGet(LogLevel.Debug)?.Log($"SendingAck: {previousTransmissionId}, {startGene} - {endGene}");

                if (previousTransmissionId != 0)
                {
                    var span = owner.ByteArray.AsSpan(position);
                    BitConverter.TryWriteBytes(span, startGene);
                    span = span.Slice(sizeof(int));
                    BitConverter.TryWriteBytes(span, endGene);
                    position += 8;
                    numberOfPairs++;

                    span = owner.ByteArray.AsSpan(transmissionPosition);
                    BitConverter.TryWriteBytes(span, previousTransmissionId);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, numberOfPairs);

                    previousTransmissionId = 0;
                    numberOfPairs = 0;
                    transmissionPosition = 0;
                    startGene = -1;
                    endGene = -1;
                }

                connection.CreateAckPacket(owner, position - PacketHeader.Length, out var packetLength);
                netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, owner.ToMemoryOwner(0, packetLength));
                // owner = owner.Return(); // Moved
            }
        }
    }
}
