// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Netsphere.Net;

internal class CubicCongestionControl : ICongestionControl
{
    private const int InitialCwnd = 10;
    private const double BurstRatio = 1.5d;
    private const double BurstRatioInv = 1.0d / BurstRatio;

    public CubicCongestionControl(Connection connection)
    {
        this.Connection = connection;
        this.cwnd = InitialCwnd;
        this.regen = this.cwnd / connection.SmoothedRtt * 1000;
        this.logger = this.Connection.ConnectionTerminal.NetBase.UnitLogger.GetLogger<CubicCongestionControl>();
    }

    #region FieldAndProperty

    public Connection Connection { get; set; }

    public int NumberOfGenesInFlight
        => this.genesInFlight.Count;

    public bool IsCongested
        => this.genesInFlight.Count >= (int)this.capacity;

    private readonly ILogger logger;

    private readonly object syncObject = new();
    private readonly UnorderedLinkedList<SendGene> genesInFlight = new(); // Retransmission mics, gene

    private double cwnd; // Upper limit of the total number of genes in-flight.

    // Smoothing transmissions
    private double capacity; // Equivalent to cwnd, but increases gradually to prevent mass transmission at once.
    private double regen; // The number of packets that can be transmitted per ms.
    private double burst; // The number of packets that can be burst-transmitted per ms.
    private long lastProcessMics;
    private long burstMicsMax;
    private long burstMics;

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ICongestionControl.AddInFlight(SendGene sendGene, long rto)
    {
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
        if (this.Connection.IsDisposed)
        {// The connection is disposed.
            return false;
        }

        // CongestionControl
        var elapsedMics = this.lastProcessMics == 0 ? 0 : Mics.FastSystem - this.lastProcessMics;
        var intCapacity = (int)this.capacity;
        if (intCapacity <= this.genesInFlight.Count)
        {// Capacity limited
            if (this.burstMics > 0)
            {// Burst
                this.capacity += this.burst;
                this.burstMics -= elapsedMics;
            }
            else
            {// Normal
                this.capacity += this.regen;
            }

            if (this.capacity > this.cwnd)
            {
                this.capacity = this.cwnd;
            }
        }
        else
        {// Not capacity limited (capacity > this.genesInFlight.Count).
            var upperLimit = this.genesInFlight.Count + this.burst;
            if (this.capacity > upperLimit)
            {
                this.capacity = upperLimit;
            }

            this.burstMics += elapsedMics;
            if (this.burstMics > this.burstMicsMax)
            {
                this.burstMics = this.burstMicsMax;
            }
        }

        this.logger.TryGet(LogLevel.Debug)?.Log($"current/cap/cwnd {this.NumberOfGenesInFlight}/{this.capacity:F0}/{this.cwnd:F0} regen {this.regen:F2} burst/mics {this.burst:F2}/{this.burstMics}");

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

    private void UpdateSmoothing(double regen)
    {
        this.regen = regen;
        this.burst = regen * BurstRatio;
        this.burstMicsMax = (long)(10_000 * BurstRatioInv); // RTT / BurstRatio
    }
}
