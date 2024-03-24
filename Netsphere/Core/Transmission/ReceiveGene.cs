// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
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

    public DataControl DataControl { get; private set; }

    public ByteArrayPool.MemoryOwner Packet { get; private set; }

    public bool IsReceived => this.DataControl != DataControl.Initial;

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRecv(DataControl dataControl, ByteArrayPool.MemoryOwner toBeShared)
    {
        if (!this.IsReceived)
        {
            this.DataControl = dataControl;
            this.Packet = toBeShared.IncrementAndShare();
        }
    }

    public void Dispose()
    {
        this.DataControl = DataControl.Initial;
        this.Packet = this.Packet.Return();
    }
}
