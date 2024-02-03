// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere.Net;

/*public sealed class StreamContext : ReceiveStream
{
    internal StreamContext(ReceiveTransmission receiveTransmission, ulong dataId, long maxStreamLength)
        : base(receiveTransmission, dataId, maxStreamLength)
    {
    }

    public NetResult SendAndForget<TSend>(TSend data, ulong dataId = 0)
    {
        var connection = this.ReceiveTransmission.Connection;
        if (connection.IsClosedOrDisposed)
        {
            return NetResult.Closed;
        }
        else if (this.State != StreamState.Received)
        {
            return NetResult.Closed;
        }

        if (connection.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        if (!BlockService.TrySerialize(data, out var owner))
        {
            return NetResult.SerializationError;
        }

        var transmission = connection.TryCreateSendTransmission(this.ReceiveTransmission.TransmissionId);
        if (transmission is null)
        {
            owner.Return();
            return NetResult.NoTransmission;
        }

        this.State = StreamState.Sent;
        var result = transmission.SendBlock(0, dataId, owner, default);
        owner.Return();
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }
}*/
