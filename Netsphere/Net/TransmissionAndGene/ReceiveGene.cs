// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Netsphere.Net;

[ValueLinkObject(Restricted = true)]
internal partial class ReceiveGene
{// lock (transmission.syncObject)
    [Link(Primary = true, Type = ChainType.SlidingList, Name = "DataPositionList")]
    public ReceiveGene(ReceiveTransmission receiveTransmission)
    {
        this.ReceiveTransmission = receiveTransmission;
    }

    #region FieldAndProperty

    public ReceiveTransmission ReceiveTransmission { get; }

    public ByteArrayPool.MemoryOwner Packet { get; private set; }

    public bool IsReceived
        => !this.Packet.IsEmpty;

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRecv(ByteArrayPool.MemoryOwner toBeShared)
    {
        if (!this.IsReceived)
        {
            this.Packet = toBeShared.IncrementAndShare();
        }
    }

    public void Dispose()
    {
        this.Packet = this.Packet.Return();
    }
}
