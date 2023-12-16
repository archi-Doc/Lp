// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Netsphere.Net;

public class FlowControl
{
    public static readonly FlowControl Default = new(NetConstants.SendCapacityPerRound);

    public FlowControl()
    {
    }

    public FlowControl(int sendCapacityPerRound)
    {
        this.sendCapacityPerRound = sendCapacityPerRound;
    }

    #region FieldAndProperty

    private readonly int sendCapacityPerRound;

    private readonly object syncObject = new();
    private readonly ConcurrentQueue<NetGene> waitingToSend = new();
    private readonly OrderedMultiMap<long, NetGene> waitingForAck = new();
    // private readonly SortedDictionary<long, NetGene> waitingForAck = new();

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddSend_LockFree(NetGene gene)
    {
        this.waitingToSend.Enqueue(gene);
    }

    internal void ProcessSend(NetSender netSender)
    {
        lock (this.syncObject)
        {
            while (netSender.SendCapacity > netSender.SendCount)
            {// Retransmission
                var firstNode = this.waitingForAck.First;
                if (firstNode is null ||
                    firstNode.Key < netSender.CurrentSystemMics)
                {
                    break;
                }

                var rto = firstNode.Value.Send_NotThreadSafe(netSender);
                if (rto > 0)
                {// Resend
                    this.waitingForAck.SetNodeKey(firstNode, rto);
                }
                else
                {// Remove
                    this.waitingForAck.RemoveNode(firstNode);
                }
            }

            // Send queue (ConcurrentQueue)
            while (netSender.SendCapacity > netSender.SendCount)
            {
                if (!this.waitingToSend.TryDequeue(out var gene))
                {// No send queue
                    return;
                }

                var rto = gene.Send_NotThreadSafe(netSender);
                if (rto > 0)
                {// Success
                    this.waitingForAck.Add(rto, gene);
                }
            }
        }
    }
}
