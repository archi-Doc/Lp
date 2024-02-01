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

    public int NumberInFlight
        => this.genesInFlight.Count;

    public bool IsCongested
        => false;

    private readonly object syncObject = new();
    private readonly OrderedMultiMap<long, SendGene> genesInFlight = new(); // Retransmission mics, gene

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ICongestionControl.AddInFlight(SendGene sendGene, int additional)
    {
        lock (this.syncObject)
        {
            var rto = Mics.FastSystem + sendGene.SendTransmission.Connection.TaichiTimeout + additional;
            if (sendGene.Node is OrderedMultiMap<long, SendGene>.Node node)
            {
                this.genesInFlight.SetNodeKey(node, rto);
            }
            else
            {
                (sendGene.Node, _) = this.genesInFlight.Add(rto, sendGene);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ICongestionControl.RemoveInFlight(SendGene sendGene, bool ack)
    {
        lock (this.syncObject)
        {
            sendGene.SendTransmission.Connection.ResetTaichi();
            if (sendGene.Node is OrderedMultiMap<long, SendGene>.Node node)
            {
                this.genesInFlight.RemoveNode(node);
                sendGene.Node = default;
            }
        }
    }

    void ICongestionControl.LossDetected(Netsphere.Net.SendGene sendGene)
    {
    }

    /*void ICongestionControl.ReportDeliverySuccess()
    {
    }

    void ICongestionControl.ReportDeliveryFailure()
    {
    }*/

    bool ICongestionControl.Process(NetSender netSender, long elapsedMics, double elapsedMilliseconds)
    {// lock (ConnectionTerminal.CongestionControlList)
        // Resend
        SendGene? gene;
        lock (this.syncObject)
        {// To prevent deadlocks, the lock order for CongestionControl must be the lowest, and it must not acquire locks by calling functions of other classes.
            int addition = 0; // Increment rto (retransmission timeout) to create a small difference.
            while (netSender.CanSend)
            {// Retransmission
                var firstNode = this.genesInFlight.First;
                if (firstNode is null ||
                    firstNode.Key > Mics.FastSystem)
                {
                    break;
                }

                gene = firstNode.Value;
                gene.SendTransmission.Connection.DoubleTaichi();
                Console.WriteLine($"RESEND: {gene.GeneSerial}/{gene.SendTransmission.GeneSerialMax}");
                if (!gene.Resend_NotThreadSafe(netSender, addition++))
                {// Cannot send
                    this.genesInFlight.RemoveNode(firstNode);
                    gene.Node = default;
                }
            }
        }

        return true; // Do not dispose NoCongestionControl as it is shared across the connections.
    }

    void ICongestionControl.AddRtt(int rttMics)
    {
    }
}
