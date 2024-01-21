// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;

#pragma warning disable SA1401 // Fields should be private

namespace Netsphere.Net;

[ValueLinkObject(Restricted = true)]
internal partial class SendGene
{// lock (transmission.syncObject)
    [Link(Primary = true, Type = ChainType.SlidingList, Name = "GeneSerialList")]
    public SendGene(SendTransmission sendTransmission)
    {
        this.SendTransmission = sendTransmission;
        this.CongestionControl = sendTransmission.Connection.GetCongestionControl(); // Keep a CongestionContro instance as a member variable, since it may be subject to change.
    }

    #region FieldAndProperty

    public SendTransmission SendTransmission { get; }

    public ICongestionControl CongestionControl { get; }

    public ByteArrayPool.MemoryOwner Packet { get; private set; }

    public long SentMics { get; private set; }

    public bool IsResend { get; private set; }

    public bool IsSent
        => this.SentMics != 0;

    public int GeneSerial
        => this.GeneSerialListLink.Position;

    internal object? Node; // lock (this.CongestionControl.syncObject)

    #endregion

    public bool CanSend
        => this.SendTransmission.Mode != NetTransmissionMode.Disposed &&
        this.SendTransmission.Connection.State == Connection.ConnectionState.Open;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSend(ByteArrayPool.MemoryOwner toBeMoved)
    {
        this.Packet = toBeMoved;
    }

    public bool TrySetResend()
    {
        if (this.SentMics != 0)
        {
            var threshold = this.SendTransmission.Connection.MinimumRtt;
            if (Mics.FastSystem - this.SentMics < threshold)
            {// Suppress the resending.
                return false;
            }
        }

        Console.WriteLine($"TrySetResend: {this.GeneSerial}");
        this.IsResend = true;
        this.SentMics = 0;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Resend_NotThreadSafe(NetSender netSender, int additional)
    {
        if (this.SentMics != 0)
        {
            var threshold = this.SendTransmission.Connection.MinimumRtt;
            if (Mics.FastSystem - this.SentMics < threshold)
            {// Suppress the resending.
                return true;
            }
        }

        this.IsResend = true;
        return this.Send_NotThreadSafe(netSender, additional);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Send_NotThreadSafe(NetSender netSender, int additional)
    {
        if (!this.CanSend || !this.Packet.TryIncrement())
        {// MemoryOwner has been returned to the pool (Disposed).
            return false;
        }

        var connection = this.SendTransmission.Connection;
        var currentMics = Mics.FastSystem;
        if (this.IsResend)
        {
            connection.IncrementResendCount();
        }
        else
        {
            connection.IncrementSendCount();
        }

        netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, this.Packet); // Incremented
        this.SentMics = currentMics;

        this.CongestionControl.AddInFlight(this, currentMics + connection.RetransmissionTimeout + additional);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose(bool ack)
    {// lock (SendTransmissions.syncObject)
        this.CongestionControl.RemoveInFlight(this, ack);
        this.Goshujin = null;
        this.Packet.Return();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DisposeMemory()
    {// lock (SendTransmissions.syncObject)
        this.CongestionControl.RemoveInFlight(this, false);
        this.Packet.Return();
    }

    public override string ToString()
        => $"Send gene {this.GeneSerial}";
}
