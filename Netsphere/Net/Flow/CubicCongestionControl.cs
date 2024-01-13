// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Netsphere.Net;

internal class CubicCongestionControl : ICongestionControl
{
    public CubicCongestionControl(Connection connection)
    {
        this.Connection = connection;
    }

    #region FieldAndProperty

    public Connection Connection { get; set; }

    public int NumberOfGenesInFlight
        => this.genesInFlight.Count;

    public bool IsCongested
        => this.sentCountPerRound >= this.maxSentCountPerRound;

    private readonly object syncObject = new();
    private readonly UnorderedLinkedList<SendGene> genesInFlight = new(); // Retransmission mics, gene

    private int sentCountPerRound;
    private int maxSentCountPerRound;

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ICongestionControl.AddInFlight(SendGene sendGene, long rto)
    {
        this.sentCountPerRound++;
        lock (this.syncObject)
        {
            if (sendGene.Node is UnorderedLinkedList<SendGene>.Node node)
            {
                this.genesInFlight.MoveToLast(node);
            }
            else
            {
                sendGene.Node = this.genesInFlight.AddLast(sendGene);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ICongestionControl.RemoveInFlight(SendGene sendGene)
    {
        lock (this.syncObject)
        {
            if (sendGene.Node is UnorderedLinkedList<SendGene>.Node node)
            {
                this.genesInFlight.Remove(node);
                sendGene.Node = default;
            }
        }
    }

    void ICongestionControl.Report()
    {
    }

    bool ICongestionControl.Process(NetSender netSender)
    {// lock (ConnectionTerminal.CongestionControlList)
        if (this.Connection.IsClosedOrDisposed)
        {// The connection is closed.
            return false;
        }

        // CongestionControl
        this.sentCountPerRound = 0;
        this.maxSentCountPerRound = 2;

        // Resend
        SendGene? gene;
        lock (this.syncObject)
        {// To prevent deadlocks, the lock order for CongestionControl must be the lowest, and it must not acquire locks by calling functions of other classes.
            while (netSender.CanSend)
            {// Retransmission
                var firstNode = this.genesInFlight.First;
                if (firstNode is null)
                {
                    break;
                }

                gene = firstNode.Value;
                if ((Mics.FastSystem - gene.SendTransmission.Connection.RetransmissionTimeout) < firstNode.Value.SentMics)
                {
                    break;
                }

                if (!gene.Send_NotThreadSafe(netSender, 0))
                {// Cannot send
                    this.genesInFlight.Remove(firstNode);
                    gene.Node = default;
                }
            }
        }

        return true;
    }

    public override string ToString()
        => $"Cubic: Per round {this.sentCountPerRound}/{this.maxSentCountPerRound}, In-flight {this.genesInFlight.Count}";
}
