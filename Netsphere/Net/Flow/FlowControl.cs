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

    public object SyncObject => this.syncObject;

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
        lock (this.syncObject)
        {
            // Retransmission timeout
            while (netSender.SendCapacity > netSender.SendCount)
            {
                var firstNode = this.waitingForAck.First;
                if (firstNode is null ||
                    firstNode.Key < netSender.CurrentSystemMics)
                {
                    return;
                }

                if (firstNode.Value.Send(netSender, out var sentCount))
                {// Resend
                    this.waitingForAck.SetNodeKey(firstNode, 222);
                }
                else
                {// Remove
                    this.waitingForAck.RemoveNode(firstNode);
                }
            }
        }

        // Send queue
        while (netSender.SendCapacity > netSender.SendCount)
        {
            if (!this.waitingToSend.TryDequeue(out var gene))
            {// No send queue
                return;
            }

            if (gene.Send(netSender, out _))
            {// Success
                (gene.rtoNode, _) = this.waitingForAck.Add(1, gene);
            }
        }
    }
}
