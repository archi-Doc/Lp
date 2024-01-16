// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Netsphere.Net;

public class CubicCongestionControl : ICongestionControl
{
    private const int CubicThreshold = 10;
    private const int InitialCwnd = 10;
    private const int MaxCwnd = 1_000_000;
    private const double BurstRatio = 1.5d;
    private const double BurstRatioInv = 1.0d / BurstRatio;
    private const double Beta = 0.2d;
    private const double WtcpRatio = 3d * Beta / (2d - Beta);

    public CubicCongestionControl(Connection connection)
    {
        this.Connection = connection;
        this.cwnd = InitialCwnd;
        this.ssthresh = InitialCwnd;
        this.slowstart = true;
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
        => this.genesInFlight.Count >= this.capacityInt;

    private readonly ILogger? logger;

    private readonly object syncObject = new();
    private readonly UnorderedLinkedList<SendGene> genesInFlight = new(); // Retransmission mics, gene

    // Smoothing transmissions
    private double capacity; // Equivalent to cwnd, but increases gradually to prevent mass transmission at once.
    private int capacityInt; // (int)this.capacity
    private double regen; // The number of packets that can be transmitted per ms.
    private double boost; // The number of packets that can be boost-transmitted per ms.
    private long boostMicsMax;
    private long boostMics;

    // Cubic
    private bool capacityLimited;
    private int cubicCount;
    private int ackCount;
    private double increaseRatio; // 1 / this.cnt
    private double cwnd; // Upper limit of the total number of genes in-flight.
    private double tcpCwnd;
    private bool slowstart;
    private int ssthresh;

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
    void ICongestionControl.RemoveInFlight(SendGene sendGene, bool ack)
    {
        lock (this.syncObject)
        {
            if (ack)
            {
                this.ackCount++;
            }

            if (sendGene.Node is UnorderedLinkedList<SendGene>.Node node)
            {
                this.genesInFlight.Remove(node);
                sendGene.Node = default;
            }
        }
    }

    void ICongestionControl.ReportAcked(int acked)
    {
    }

    bool ICongestionControl.Process(NetSender netSender, long elapsedMics, double elapsedMilliseconds)
    {// lock (ConnectionTerminal.CongestionControlList)
        if (this.Connection.IsDisposed)
        {// The connection is disposed.
            return false;
        }

        // CongestionControl: Cubic (cwnd)
        this.capacityLimited |= (this.genesInFlight.Count * 8) >= (this.capacityInt * 7);
        if (this.cubicCount++ >= CubicThreshold)
        {
            if (this.capacityLimited)
            {// Capacity limited
                this.capacityLimited = false;
                this.UpdateCubic((double)this.ackCount);
            }

            this.cubicCount = 0;
            this.ackCount = 0;
        }

        // CongestionControl: Capacity
        var capacityLimited = this.CalculateCapacity(elapsedMics, elapsedMilliseconds);
        this.capacityInt = (int)this.capacity;

        this.logger?.TryGet(LogLevel.Debug)?.Log($"{(capacityLimited ? "CAP " : string.Empty)}current/cap/cwnd {this.NumberOfGenesInFlight}/{this.capacity:F1}/{this.cwnd:F1} regen {this.regen:F2} boost {this.boost:F2} mics/max {this.boostMics}/{this.boostMicsMax}");

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

    private bool CalculateCapacity(long elapsedMics, double elapsedMilliseconds)
    {
        if (this.capacityInt <= this.genesInFlight.Count)
        {// Capacity limited
            if (this.boostMics > 0)
            {// Boost
                this.capacity += this.boost * elapsedMilliseconds;

                if (this.capacity >= this.cwnd)
                {
                    this.capacity = this.cwnd;

                    this.boostMics += elapsedMics;
                    if (this.boostMics > this.boostMicsMax)
                    {
                        this.boostMics = this.boostMicsMax;
                    }

                    return true;
                }
                else
                {
                    this.boostMics -= elapsedMics;
                    return true;
                }
            }
            else
            {// Normal
                this.capacity += this.regen * elapsedMilliseconds;

                if (this.capacity >= this.cwnd)
                {
                    this.capacity = this.cwnd;

                    this.boostMics += elapsedMics;
                    if (this.boostMics > this.boostMicsMax)
                    {
                        this.boostMics = this.boostMicsMax;
                    }

                    return true;
                }
                else
                {
                    return true;
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

            return false;
        }
    }

    private void UpdateCubic(double acked)
    {
        if (this.slowstart)
        {// Slow start
            var cwnd = Math.Min(this.cwnd + acked, this.ssthresh);
            acked -= cwnd - this.cwnd;
            this.cwnd = cwnd; // MaxCwnd

            if (acked == 0)
            {
                return;
            }
        }

TcpFriendliness:
        this.tcpCwnd += WtcpRatio * acked / this.cwnd;
        if (this.tcpCwnd > this.cwnd)
        {
            var ratio = (this.tcpCwnd - this.cwnd) / this.cwnd;
            if (this.increaseRatio < ratio)
            {
                this.increaseRatio = ratio;
            }
        }

        this.increaseRatio = Math.Min(this.increaseRatio, 0.5);
        this.cwnd += acked * this.increaseRatio; // MaxCwnd
    }

    private void UpdateSmoothing(double regen)
    {
        this.regen = regen;
        this.boost = regen * BurstRatio;
        this.boostMicsMax = (long)(this.Connection.SmoothedRtt * BurstRatioInv); // RTT / BurstRatio
    }
}
