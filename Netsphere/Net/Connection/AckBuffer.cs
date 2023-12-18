// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Netsphere.Packet;
using static Arc.Unit.ByteArrayPool;

namespace Netsphere.Net;

internal partial class AckBuffer
{
    private readonly record struct ConnectionAndAckQueue(Connection connection, Queue<long> queue);

    public AckBuffer()
    {
    }

    #region FieldAndProperty

    private readonly object syncObject = new();
    private readonly Queue<Queue<ulong>> freeQueue = new();
    private readonly Queue<Connection> connectionQueue = new();

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Connection connection, uint transmissionId, int geneSerial)
    {
        lock (this.syncObject)
        {
            var queue = connection.AckQueue;
            if (queue is null)
            {
                this.freeQueue.TryDequeue(out queue);
                queue ??= new();
                this.connectionQueue.Enqueue(connection);
                connection.AckMics = Mics.GetSystem() + NetConstants.AckDelayMics;
                connection.AckQueue = queue;
            }

            queue.Enqueue((transmissionId << 32) | (uint)geneSerial);
        }
    }

    public void ProcessSend(NetSender netSender)
    {
        Connection? connection = default;
        Queue<ulong>? ackQueue = default;

        while (netSender.CanSend)
        {
            lock (this.syncObject)
            {
                if (ackQueue is not null)
                {
                    this.freeQueue.Enqueue(ackQueue);
                    ackQueue = default;
                }

                this.connectionQueue.TryPeek(out connection);
                if (connection is not null && netSender.CurrentSystemMics > connection.AckMics)
                {
                    this.connectionQueue.Dequeue();
                    ackQueue = connection.AckQueue;
                    connection.AckMics = 0;
                    connection.AckQueue = default;
                }
            }

            if (connection is null || ackQueue is null)
            {
                break;
            }

            this.ProcessSend(netSender, connection, ackQueue);
        }
    }

    private void ProcessSend(NetSender netSender, Connection connection, Queue<ulong> ackQueue)
    {// AckFrameCode
        ByteArrayPool.Owner? arrayOwner = default;
        Span<byte> span = default;
        uint previousTransmissionId = 0;
        int numberOfPairs = 0;
        int startGene = -1;
        int endGene = -1;

        while (ackQueue.TryDequeue(out var ack))
        {
            if (span.Length < AckFrame.Margin)
            {
                SendPacket();
            }

            if (arrayOwner is null)
            {
                arrayOwner = PacketPool.Rent();
                span = arrayOwner.ByteArray.AsSpan(PacketHeader.Length + AckFrame.Length, AckFrame.MaxGeneLength);
            }

            var transmissionId = (uint)(ack >> 32);
            var geneSerial = (int)ack;

            if (previousTransmissionId != 0 &&
                transmissionId == previousTransmissionId)
            {// Same transmission id
                if (startGene == -1 && endGene == -1)
                {// Initial
                    startGene = geneSerial;
                    endGene = geneSerial;
                }
                else if (endGene == geneSerial - 1)
                {// Serial genes
                    endGene = geneSerial;
                }
                else
                {// Not serial
                    BitConverter.TryWriteBytes(span, startGene);
                    span = span.Slice(sizeof(int));
                    BitConverter.TryWriteBytes(span, endGene);
                    span = span.Slice(sizeof(int));
                    numberOfPairs++;

                    startGene = -1;
                    endGene = -1;
                }
            }
            else
            {// New transmission id
            }
        }

        SendPacket();

        void SendPacket()
        {
            if (arrayOwner is not null)
            {
                connection.CreateAckPacket(arrayOwner, AckFrame.MaxGeneLength - span.Length, out var packetLength);
                netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, arrayOwner.ToMemoryOwner());
                arrayOwner = arrayOwner.Return();
            }
        }
    }
}
