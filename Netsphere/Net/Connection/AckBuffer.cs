// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

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

            this.ProcessSend(connection, ackQueue);
        }
    }

    private void ProcessSend(Connection connection, Queue<ulong> ackQueue)
    {
        while (ackQueue.TryDequeue(out var ack))
        {
        }
    }
}
