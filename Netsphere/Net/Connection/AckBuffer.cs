// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
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
    private readonly ConcurrentQueue<Queue<int>> freeAckGene = new();

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
                this.freeAckGene.TryDequeue(out ackGene); // Reuse the queued queue.
                ackGene ??= new();

                receiveTransmission.AckGene = ackGene;
                ackQueue.Enqueue(new(receiveTransmission, ackGene));
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

        ByteArrayPool.Owner? owner = default;
        Span<byte> span = default;

        while (ackQueue.TryDequeue(out var item))
        {
NewPacket:
            if (owner is not null && span.Length < AckFrame.Margin)
            {// Send the packet when the remaining length falls below the margin.
                Send(maxLength - span.Length);
            }

            if (owner is null)
            {// Prepare
                owner = PacketPool.Rent();
                span = owner.ByteArray.AsSpan(PacketHeader.Length + 2, maxLength); // PacketHeader, FrameType, NumberOfRama, NumberOfBlock
            }

            var ackGene = item.AckGene;
            if (ackGene == this.rama)
            {// Rama
                BitConverter.TryWriteBytes(span, (int)-1);
                span = span.Slice(sizeof(int));
                BitConverter.TryWriteBytes(span, item.ReceiveTransmission.TransmissionId);
                span = span.Slice(sizeof(uint));
            }
            else
            {// Block/Stream
                int receiveCapacity = 0;
                int successiveReceivedPosition = 0;

                BitConverter.TryWriteBytes(span, receiveCapacity);
                span = span.Slice(sizeof(int));
                BitConverter.TryWriteBytes(span, item.ReceiveTransmission.TransmissionId);
                span = span.Slice(sizeof(uint));
                BitConverter.TryWriteBytes(span, successiveReceivedPosition);
                span = span.Slice(sizeof(int));

                ushort numberOfPairs = 0;
                var numberOfPairsSpan = span;
                span = span.Slice(sizeof(ushort));

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
                        BitConverter.TryWriteBytes(span, startGene);
                        span = span.Slice(sizeof(int));
                        BitConverter.TryWriteBytes(span, endGene);
                        span = span.Slice(sizeof(int));
                        numberOfPairs++;

                        if (owner is not null && span.Length < AckFrame.Margin)
                        {// Send the packet when the remaining length falls below the margin.
                            BitConverter.TryWriteBytes(numberOfPairsSpan, numberOfPairs);
                            goto NewPacket;
                        }

                        startGene = geneSerial;
                        endGene = geneSerial + 1;
                    }
                }

                if (startGene != -1)
                {
                    BitConverter.TryWriteBytes(span, startGene);
                    span = span.Slice(sizeof(int));
                    BitConverter.TryWriteBytes(span, endGene);
                    span = span.Slice(sizeof(int));
                    numberOfPairs++;
                    BitConverter.TryWriteBytes(numberOfPairsSpan, numberOfPairs);
                }

                this.freeAckGene.Enqueue(ackGene);
            }
        }

        if (owner is not null && span.Length > 0)
        {// Send the packet if not empty.
            Send(maxLength - span.Length);
        }

        owner?.Return(); // Return the rent buffer.
        // ackQueue will be returned later for reuse.

        void Send(int spanLength)
        {
            connection.CreateAckPacket(owner, spanLength, out var packetLength);
            netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, owner.ToMemoryOwner(0, packetLength));
            // owner = owner.Return(); // Moved
            owner = default;
        }
    }
}
