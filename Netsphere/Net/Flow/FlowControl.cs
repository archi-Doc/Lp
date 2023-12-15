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

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddSend(NetGene gene)
    {
        this.waitingToSend.Enqueue(gene);
    }

    internal void ProcessSend(NetSender netSender)
    {
        int sentCount = 0;
        lock (this.syncObject)
        {
            while (netSender.SendCapacity > netSender.SendCount)
            {// Retransmission
                var firstNode = this.waitingForAck.First;
                if (firstNode is null ||
                    firstNode.Key < netSender.CurrentSystemMics)
                {
                    return;
                }

                if (firstNode.Value.Send(netSender, ref sentCount))
                {// Resend
                    this.waitingForAck.SetNodeKey(firstNode, 222);
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

                if (gene.Send(netSender, ref sentCount))
                {// Success
                    (gene.WaitingForAckNode, _) = this.waitingForAck.Add(1, gene);
                }
            }
        }
    }

    internal void Remove(NetGene gene)
    {
        lock (gene.FlowControl.syncObject)
        {
            if (gene.WaitingForAckNode is { } node)
            {
                this.waitingForAck.RemoveNode(node);
            }
        }
    }
}
