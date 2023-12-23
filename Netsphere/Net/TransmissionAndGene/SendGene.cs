﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using System.Runtime.CompilerServices;

namespace Netsphere.Net;

[ValueLinkObject(Restricted = true)]
internal partial class SendGene
{// lock (transmission.syncObject)
    [Link(Primary = true, Type = ChainType.SlidingList, Name = "GeneSerialList")]
    public SendGene(SendTransmission sendTransmission)
    {
        this.SendTransmission = sendTransmission;
        this.FlowControl = sendTransmission.Connection.FlowControl; // Keep FlowControl instance as a member variable, since it may be subject to change.
    }

    #region FieldAndProperty

    public SendTransmission SendTransmission { get; }

    public FlowControl FlowControl { get; }

    public ByteArrayPool.MemoryOwner Packet { get; private set; }

    public long SentMics { get; private set; }

    #endregion

    public bool CanSend
        => this.SendTransmission.Mode != NetTransmissionMode.Disposed &&
        this.SendTransmission.Connection.State == Connection.ConnectionState.Open;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSend(ByteArrayPool.MemoryOwner toBeMoved)
    {
        this.Packet = toBeMoved;
        this.FlowControl.AddSend_LockFree(this); // Lock-free
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Send_NotThreadSafe(NetSender netSender)
    {
        if (!this.CanSend)
        {
            return 0;
        }
        else if (!this.Packet.TryIncrement())
        {// MemoryOwner has been returned to the pool (Disposed).
            return 0;
        }

        var connection = this.SendTransmission.Connection;
        var currentMics = netSender.CurrentSystemMics;

        netSender.Send_NotThreadSafe(connection.EndPoint.EndPoint, this.Packet); // Incremented
        this.SentMics = currentMics;
        // Console.WriteLine($"Send: {this.Packet.Memory.Length} bytes to {connection.EndPoint.ToString()}");
        return currentMics + connection.RetransmissionTimeout;
    }

    public void Dispose()
    {
        this.Packet.Return();
    }
}