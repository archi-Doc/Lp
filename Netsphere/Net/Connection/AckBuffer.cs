// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Netsphere.Packet;

namespace Netsphere.Net;

internal partial class AckBuffer
{
    internal readonly struct ReceiveTransmissionAndAckGene
    {
        public ReceiveTransmissionAndAckGene(ReceiveTransmission transmission, Queue<int> ackGene)
        {
            this.ReceiveTransmission = transmission;
            this.AckGene = ackGene;
        }

        public readonly ReceiveTransmission ReceiveTransmission;
        public readonly Queue<int> AckGene;
    }

    public AckBuffer(ConnectionTerminal connectionTerminal)
    {
        this.connectionTerminal = connectionTerminal;
        this.logger = connectionTerminal.UnitLogger.GetLogger<AckBuffer>();
    }

    #region FieldAndProperty

    private readonly ConnectionTerminal connectionTerminal;
    private readonly ILogger logger;
    private readonly Queue<int> rama = new();

    private readonly object syncObject = new();
    private readonly Queue<Connection> connectionQueue = new();
    private readonly Queue<Queue<ReceiveTransmissionAndAckGene>> freeAckQueue = new();
    private readonly Queue<Queue<int>> freeAckGene = new();

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AckRama(Connection connection, ReceiveTransmission receiveTransmission)
    {
#if LOG_LOWLEVEL_NET
        this.logger.TryGet(LogLevel.Debug)?.Log($"AckRama {this.connectionTerminal.NetTerminal.NetTerminalString} to {connection.EndPoint.ToString()} {receiveTransmission.TransmissionId}");
#endif

        lock (this.syncObject)
        {
            var ackQueue = connection.AckQueue;
            if (ackQueue is null)
            {
                this.freeAckQueue.TryDequeue(out ackQueue); // Reuse the queued queue.
                ackQueue ??= new();

                connection.AckMics = Mics.FastSystem + NetConstants.AckDelayMics;
                connection.AckQueue = ackQueue;
                this.connectionQueue.Enqueue(connection);
            }

            var ackGene = receiveTransmission.AckGene;
            if (ackGene is null)
            {
                receiveTransmission.AckGene = this.rama;
                ackQueue.Enqueue(new(receiveTransmission, this.rama));
            }
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
            var ackBlock = connection.AckQueue;
            if (ackBlock is null)
            {
                this.freeAckQueue.TryDequeue(out ackBlock); // Reuse the queued queue.
                ackBlock ??= new();

                connection.AckMics = Mics.FastSystem + NetConstants.AckDelayMics;
                connection.AckQueue = ackBlock;
                this.connectionQueue.Enqueue(connection);
            }

            var ackGene = receiveTransmission.AckGene;
            if (ackGene is null)
            {
                this.freeAckGene.TryDequeue(out ackGene); // Reuse the queued queue.
                ackGene ??= new();

                receiveTransmission.AckGene = ackGene;
                ackBlock.Enqueue(new(receiveTransmission, ackGene));
            }

            ackGene.Enqueue(geneSerial);
        }
    }

    public void ProcessSend(NetSender netSender)
    {
        Connection? connection = default;
        Queue<ReceiveTransmissionAndAckGene>? ackQueue = default;

        while (true)
        {
            lock (this.syncObject)
            {
                if (ackQueue is not null)
                {
                    this.freeAckQueue.Enqueue(ackQueue);
                    ackQueue = default;
                }

                if (!netSender.CanSend)
                {
                    return;
                }

                this.connectionQueue.TryPeek(out connection);
                if (connection is not null && Mics.FastSystem > connection.AckMics)
                {
                    this.connectionQueue.Dequeue();
                    ackQueue = connection.AckQueue!;
                    foreach (var x in ackQueue)
                    {
                        x.ReceiveTransmission.AckGene = default;
                    }

                    connection.AckMics = 0;
                    connection.AckQueue = default;
                }
            }

            // To shorten the acquisition time of the exclusive lock, temporarily release the lock.

            if (connection is null || ackQueue is null)
            {
                break;
            }

            this.ProcessAck(netSender, connection, ackQueue);
        }
    }

    private void ProcessAck(NetSender netSender, Connection connection, Queue<ReceiveTransmissionAndAckGene> ackQueue)
    {
        const int maxLength = PacketHeader.MaxFrameLength - 2;
        ushort numberOfTransmissions = 0;

        ByteArrayPool.Owner? owner = default;
        Span<byte> span = default;

        while (ackQueue.TryDequeue(out var item))
        {
            if (owner is not null && span.Length < AckFrame.Margin)
            {// Send the packet when the remaining length falls below the margin.
                Send(span.Length);
            }

            if (owner is null)
            {// Prepare
                owner = PacketPool.Rent();
                span = owner.ByteArray.AsSpan(PacketHeader.Length + 6, maxLength); // PacketHeader, FrameType, NumberOfRama, NumberOfBlock
            }

            if (item.AckGene == this.rama)
            {// Rama
                numberOfTransmissions++;
                BitConverter.TryWriteBytes(span, item.ReceiveTransmission.TransmissionId);
                span = span.Slice(sizeof(uint));
            }
            else
            {// Block/Stream

            }
        }

        while (ackBlock.TryDequeue(out var transmission))
        {
            if (owner is not null && span.Length < AckFrame.Margin)
            {// Send the packet when the remaining length falls below the margin.
                Send(span.Length);
            }

            if (owner is null)
            {// Prepare
                owner = PacketPool.Rent();
                span = owner.ByteArray.AsSpan(PacketHeader.Length + 6, maxLength); // PacketHeader, FrameType, NumberOfRama, NumberOfBlock
            }

            var ackGene = transmission.AckGene;
            transmission.AckGene = default;
            if (ackGene is null)
            {
                continue;
            }

            var transmissionSpan = span;
            var geneSpan = span;
            var transmissionId = transmission.TransmissionId;
            int successiveReceivedPosition = 0;
            int receiveCapacity = 0;
            ushort numberOfPairs = 0;
            int startGene = -1;
            int endGene = -1;

            while (ackGene.TryDequeue(out var geneSerial))
            {
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
                    BitConverter.TryWriteBytes(geneSpan, startGene);
                    geneSpan = geneSpan.Slice(sizeof(int));
                    BitConverter.TryWriteBytes(geneSpan, endGene);
                    geneSpan = geneSpan.Slice(sizeof(int));
                    numberOfPairs++;

                    startGene = geneSerial;
                    endGene = geneSerial + 1;

                    if (owner is not null && span.Length < AckFrame.Margin)
                    {// Send the packet when the remaining length falls below the margin.
                        Send(span.Length);
                    }

                    if (owner is null)
                    {// Prepare
                        owner = PacketPool.Rent();
                        span = owner.ByteArray.AsSpan(PacketHeader.Length + 6, maxLength); // PacketHeader, FrameType, NumberOfRama, NumberOfBlock
                    }
                }
            }
        }

        if (owner is not null && span.Length > 0)
        {// Send the packet if not empty.
            Send(span.Length);
        }

        owner?.Return(); // Return the rent buffer.
                         // ackRama and ackBlock will be returned later for reuse.

        void Send(int spanLength)
        {
            connection.CreateAckPacket(owner, numberOfTransmissions, spanLength, out var packetLength);
            netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, owner.ToMemoryOwner(0, packetLength));
            // owner = owner.Return(); // Moved
            owner = default;

            numberOfTransmissions = 0;
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
