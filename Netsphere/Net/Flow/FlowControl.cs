// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class FlowControl
{
    public FlowControl(Connection connection)
    {
        this.Connection = connection;
    }

    [Link(Primary = true, Name = "List", Type = ChainType.LinkedList)]
    public FlowControl(int sendCapacityPerRound)
    {
        this.sendCapacityPerRound = sendCapacityPerRound;
        this.IsShared = true;
    }

    #region FieldAndProperty

    public Connection? Connection { get; private set; }

    public bool IsShared { get; private set; }

    public long DeletionMics { get; private set; }

    public int NumberOfGenesInFlight
        => this.genesInFlight.Count;

    private readonly int sendCapacityPerRound;

    private readonly object syncObject = new();
    private readonly ConcurrentQueue<SendGene> waitingToSend = new();
    private readonly OrderedMultiMap<long, SendGene> genesInFlight = new();

    public bool IsEmpty => this.waitingToSend.IsEmpty && this.genesInFlight.Count == 0;

    #endregion

    public void ResetDeletionMics()
        => this.DeletionMics = 0;

    public void SetDeletionMics(long mics)
        => this.DeletionMics = mics;

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

            int rtoIncrement = 0; // Increment RTO to create a small difference.
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
                    // Console.WriteLine("RESEND");
                    remaining--;
                    this.genesInFlight.SetNodeKey(firstNode, rto + (rtoIncrement++));

                    if (this.IsShared)
                    {
                        gene.SendTransmission.Connection.ReportResend();
                    }
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
                    (gene.Node, _) = this.genesInFlight.Add(rto + (rtoIncrement++), gene);
                }
                else
                {// Cannot send
                }
            }
        }
    }
}
