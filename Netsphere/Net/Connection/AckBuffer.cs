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
        Connection? connection;
        Queue<ulong>
        Queue<(Connection, Queue<ulong>)>? sendQueue = default;
        lock (this.syncObject)
        {
            this.connectionQueue.TryPeek(out var connection);
            if (connection is not null && netSender.CurrentSystemMics > connection.AckMics)
            {
                this.connectionQueue.Dequeue();
                if (connection.AckQueue is not null)
                {
                    sendQueue ??= new();
                    sendQueue.Enqueue((connection, connection.AckQueue));
                    connection.AckMics = 0;
                    connection.AckQueue = default;
                }
            }
        }

        if (sendQueue is not null)
        {
            while (sendQueue.TryDequeue(out var item))
            {
            }
        }
    }

    private void ProcessSend(Connection connection, Queue<ulong> ackQueue)
    {

    }
}

/*[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
internal partial class AckBuffer
{
    [Link(Name = "ConnectionId", Type = ChainType.Unordered, TargetMember = "ConnectionId")]
    public AckBuffer(Connection connection)
    {
        this.Connection = connection;
        this.AckTime = Mics.GetSystem() + NetConstants.AckDelayMics;
    }

    public Connection Connection { get; }

    public ulong ConnectionId
        => this.Connection.ConnectionId;

    [Link(Type = ChainType.Ordered)]
    public long AckTime { get; set; }
}*/
