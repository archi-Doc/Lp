// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

#pragma warning disable SA1202 // Elements should be ordered by access

public abstract class ReceiveStreamBase
{
    internal ReceiveStreamBase(ReceiveTransmission receiveTransmission, ulong dataId)
    {
        this.ReceiveTransmission = receiveTransmission;
        this.DataId = dataId;
    }

    #region FieldAndProperty

    internal ReceiveTransmission ReceiveTransmission { get; }

    public ulong DataId { get; }

    internal int CurrentPosition { get; set; }

    #endregion

    public void Abort()
        => this.ReceiveTransmission.ProcessAbort();
}

public class ReceiveStream : ReceiveStreamBase
{
    internal ReceiveStream(ReceiveTransmission receiveTransmission, ulong dataId)
        : base(receiveTransmission, dataId)
    {
    }

    public Task<(NetResult Result, int Written)> Receive(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => this.ReceiveTransmission.ProcessReceive(this, buffer, cancellationToken);
}
