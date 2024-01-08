// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Netsphere.Net;

internal class RamaControl
{
    public RamaControl()
    {
    }

    #region FieldAndProperty

    public int NumberOfGenesInFlight
        => this.genesInFlight.Count;

    private readonly object syncObject = new();
    private readonly ConcurrentQueue<SendGene> waitingToSend = new();
    private readonly OrderedMultiMap<long, SendGene> genesInFlight = new(); // Retransmission mics, gene

    public bool IsEmpty
        => this.waitingToSend.IsEmpty && this.genesInFlight.Count == 0;

    #endregion

    public void Clear()
    {
        lock (this.syncObject)
        {
            this.Connection = default;
            this.waitingToSend.Clear();

            foreach (var x in this.genesInFlight.Values)
            {
                if (x.Node is { } node)
                {
                    // this.genesInFlight.RemoveNode(node); // Cannot modify the collection.
                    x.Node = default;
                }
            }

            this.genesInFlight.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddSend_LockFree(SendGene gene)
    {
        this.waitingToSend.Enqueue(gene);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Remove_InFlight(SendGene gene)
    {
        lock (this.syncObject)
        {
            if (gene.Node is { } node)
            {
                this.genesInFlight.RemoveNode(node);
                gene.Node = default;
            }
        }
    }

    internal void ProcessSend(NetSender netSender)
    {
        SendGene? gene;
        lock (this.syncObject)
        {
            var remaining = this.sendCapacityPerRound;

            if (remaining == 0)
            {
                remaining = 1; // tempcode
            }

            int addition = 0; // Increment RTO to create a small difference.
            while (remaining > 0 && netSender.CanSend)
            {// Retransmission
                var firstNode = this.genesInFlight.First;
                if (firstNode is null ||
                    firstNode.Key > Mics.FastSystem)
                {
                    break;
                }

                gene = firstNode.Value;
                gene.SendTransmission.CheckLatestAckMics(Mics.FastSystem);
                var rto = gene.Send_NotThreadSafe(netSender);
                if (rto > 0)
                {// Resend
                    Console.WriteLine($"RESEND:{gene.GeneSerial}, RTO:{(rto - Mics.FastSystem) / 1_000}ms");
                    remaining--;
                    this.genesInFlight.SetNodeKey(firstNode, rto + (addition++));

                    gene.SendTransmission.Connection.ReportResend();
                }
                else
                {// Cannot send
                    this.genesInFlight.RemoveNode(firstNode);
                }
            }

            // Send queue (ConcurrentQueue)
            while (remaining > 0 && netSender.CanSend)
            {
                if (!this.waitingToSend.TryDequeue(out gene))
                {// No send queue
                    return;
                }

                var rto = gene.Send_NotThreadSafe(netSender);
                if (rto > 0)
                {// Send
                    remaining--;
                    (gene.Node, _) = this.genesInFlight.Add(rto + (addition++), gene);
                    gene.SendTransmission.Connection.IncrementSentCount();
                }
                else
                {// Cannot send
                }
            }
        }
    }
}
