// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Netsphere.Net;

internal class NoCongestionControl : ICongestionControl
{
    public NoCongestionControl()
    {
    }

    #region FieldAndProperty

    public int NumberOfGenesInFlight
        => this.genesInFlight.Count;

    Connection ICongestionControl.Connection
        => throw new NotImplementedException();

    public bool IsCongested
        => false;

    private readonly object syncObject = new();
    private readonly OrderedMultiMap<long, SendGene> genesInFlight = new(); // Retransmission mics, gene

    #endregion

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
    }

    void ICongestionControl.TrySend(SendGene gene, NetSender netSender)
    {
        if (!gene.IsSent)
        {
            gene.Send_NotThreadSafe(netSender);
        }
    }

    void ICongestionControl.Report()
    {
    }

    bool ICongestionControl.Process(NetSender netSender)
    {// lock (ConnectionTerminal.CongestionControlList)
        // CongestionControl

        // Resend
        SendGene? gene;
        lock (this.syncObject)
        {
            int addition = 0; // Increment RTO to create a small difference.
            while (netSender.CanSend)
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
                    // Console.WriteLine($"RESEND:{gene.GeneSerial}, RTO:{(rto - Mics.FastSystem) / 1_000}ms");
                    this.genesInFlight.SetNodeKey(firstNode, rto + (addition++));

                    gene.SendTransmission.Connection.ReportResend();
                }
                else
                {// Cannot send
                    this.genesInFlight.RemoveNode(firstNode);
                    gene.Node = default;
                }
            }
        }

        return true;
    }
}
