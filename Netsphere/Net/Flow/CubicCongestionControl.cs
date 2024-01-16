// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Netsphere.Net;

public class CubicCongestionControl : ICongestionControl
{
    private const int InitialCwnd = 10;
    private const double BurstRatio = 1.5d;
    private const double BurstRatioInv = 1.0d / BurstRatio;

    public CubicCongestionControl(Connection connection)
    {
        this.Connection = connection;
        this.cwnd = InitialCwnd;
        var rtt = connection.SmoothedRtt;
        this.UpdateSmoothing(this.cwnd / rtt * 1000);

        if (connection.IsClient)
        {
            this.logger = this.Connection.ConnectionTerminal.NetBase.UnitLogger.GetLogger<CubicCongestionControl>();
            this.logger.TryGet()?.Log($"Cubic Congestion Control RTT: {rtt / 1000} ms");
        }
    }

    #region FieldAndProperty

    public Connection Connection { get; set; }

    public int NumberOfGenesInFlight
        => this.genesInFlight.Count;

    public bool IsCongested
        => this.genesInFlight.Count >= (int)this.capacity;

    private readonly ILogger? logger;

    private readonly object syncObject = new();
    private readonly UnorderedLinkedList<SendGene> genesInFlight = new(); // Retransmission mics, gene

    private double cwnd; // Upper limit of the total number of genes in-flight.

    // Smoothing transmissions
    private double capacity; // Equivalent to cwnd, but increases gradually to prevent mass transmission at once.
    private double regen; // The number of packets that can be transmitted per ms.
    private double boost; // The number of packets that can be boost-transmitted per ms.
    private long boostMicsMax;
    private long boostMics;

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

    bool ICongestionControl.Process(NetSender netSender, long elapsedMics, double elapsedMilliseconds)
    {// lock (ConnectionTerminal.CongestionControlList)
        if (this.Connection.IsDisposed)
        {// The connection is disposed.
            return false;
        }

        // CongestionControl
        this.ProcessCapacity(elapsedMics, elapsedMilliseconds);

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

    private void ProcessCapacity(long elapsedMics, double elapsedMilliseconds)
    {
        var intCapacity = (int)this.capacity;
        var capLimit = false;
        if (intCapacity <= this.genesInFlight.Count)
        {// Capacity limited
            capLimit = true;

            if (this.boostMics > 0)
            {// Burst
                this.capacity += this.boost * elapsedMilliseconds;

                if (this.capacity > this.cwnd)
                {
                    this.capacity = this.cwnd;

                    this.boostMics += elapsedMics;
                    if (this.boostMics > this.boostMicsMax)
                    {
                        this.boostMics = this.boostMicsMax;
                    }
                }
                else
                {
                    this.boostMics -= elapsedMics;
                }
            }
            else
            {// Normal
                this.capacity += this.regen * elapsedMilliseconds;

                if (this.capacity > this.cwnd)
                {
                    this.capacity = this.cwnd;

                    this.boostMics += elapsedMics;
                    if (this.boostMics > this.boostMicsMax)
                    {
                        this.boostMics = this.boostMicsMax;
                    }
                }
            }
        }
        else
        {// Not capacity limited (capacity > this.genesInFlight.Count).
            var upperLimit = this.genesInFlight.Count + this.boost;
            if (this.capacity > upperLimit)
            {
                this.capacity = upperLimit;
            }

            this.boostMics += elapsedMics;
            if (this.boostMics > this.boostMicsMax)
            {
                this.boostMics = this.boostMicsMax;
            }
        }

        this.logger?.TryGet(LogLevel.Debug)?.Log($"{(capLimit ? "CAP " : string.Empty)}current/cap/cwnd {this.NumberOfGenesInFlight}/{this.capacity:F1}/{this.cwnd:F1} regen {this.regen:F2} boost {this.boost:F2} mics/max {this.boostMics}/{this.boostMicsMax}");
    }

    private void UpdateSmoothing(double regen)
    {
        this.regen = regen;
        this.boost = regen * BurstRatio;
        this.boostMicsMax = (long)(this.Connection.SmoothedRtt * BurstRatioInv); // RTT / BurstRatio
    }
}
