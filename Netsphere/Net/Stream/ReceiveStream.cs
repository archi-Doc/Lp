// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

#pragma warning disable SA1202 // Elements should be ordered by access

public class ReceiveStream
{
    internal ReceiveStream(ReceiveTransmission receiveTransmission, ulong dataId, long maxStreamLength)
    {
        this.ReceiveTransmission = receiveTransmission;
        this.DataId = dataId;
        this.MaxStreamLength = maxStreamLength;
    }

    #region FieldAndProperty

    public StreamState State { get; internal set; }

    internal ReceiveTransmission ReceiveTransmission { get; }

    public ulong DataId { get; }

    public long MaxStreamLength { get; internal set; }

    public long ReceivedLength { get; internal set; }

    internal int CurrentPosition { get; set; }

    #endregion

    public void Abort()
        => this.ReceiveTransmission.ProcessAbort();

    public Task<(NetResult Result, int Written)> Receive(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => this.ReceiveTransmission.ProcessReceive(this, buffer, cancellationToken);
}
