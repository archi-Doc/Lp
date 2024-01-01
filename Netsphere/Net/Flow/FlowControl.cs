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

    internal bool MarkedForDeletion { get; set; }

    private readonly int sendCapacityPerRound;

    private readonly object syncObject = new();
    private readonly ConcurrentQueue<SendGene> waitingToSend = new();
    private readonly OrderedMultiMap<long, SendGene> waitingForAck = new();
    // private readonly SortedDictionary<long, NetGene> waitingForAck = new();

    public bool IsEmpty => this.waitingToSend.IsEmpty && this.waitingForAck.Count == 0;

    #endregion

    public void Clear()
    {
        lock (this.syncObject)
        {
            this.Connection = default;
            this.waitingToSend.Clear();
            this.waitingForAck.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddSend_LockFree(SendGene gene)
    {
        this.waitingToSend.Enqueue(gene);
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

            int rtoSerial = 0; // Increment RTO to create a small difference.
            while (remaining > 0 && netSender.CanSend)
            {// Retransmission
                var firstNode = this.waitingForAck.First;
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
                    this.waitingForAck.SetNodeKey(firstNode, rto + (rtoSerial++));

                    if (this.IsShared)
                    {
                        gene.SendTransmission.Connection.ReportResend();
                    }
                }
                else
                {// Cannot send
                    this.waitingForAck.RemoveNode(firstNode);
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
                    this.waitingForAck.Add(rto + (rtoSerial++), gene);
                }
            }
        }
    }
}
