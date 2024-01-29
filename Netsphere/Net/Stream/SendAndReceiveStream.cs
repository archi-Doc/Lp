// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace Netsphere.Net;

internal class SendAndReceiveStream : SendStreamBase
{
    internal SendAndReceiveStream(SendTransmission sendTransmission, long maxLength, ulong dataId)
        : base(sendTransmission, maxLength, dataId)
    {
    }

    public async Task<NetResultValue<ReceiveStream>> CompleteAndReceive(CancellationToken cancellationToken = default)
    {
        if (this.IsComplete)
        {
            return new(NetResult.Completed);
        }

        await this.SendTransmission.ProcessSend(this, ReadOnlyMemory<byte>.Empty, cancellationToken);

        this.SendTransmission.Dispose();
        this.IsComplete = true;

        NetResponse response;
        var connection = this.SendTransmission.Connection;
        var transmissionId = this.SendTransmission.TransmissionId;
        var timeout = connection.NetBase.DefaultSendTimeout;
        var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var receiveTransmission = connection.TryCreateReceiveTransmission(transmissionId, tcs))
        {
            if (receiveTransmission is null)
            {
                return new(NetResult.NoTransmission);
            }

            try
            {
                response = await tcs.Task.WaitAsync(timeout, connection.CancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                if (response.IsFailure)
                {
                    return new(response.Result);
                }
            }
            catch (TimeoutException)
            {
                return new(NetResult.Timeout);
            }
            catch
            {
                return new(NetResult.Canceled);
            }
        }

        response.Return();
        return new(NetResult.Success);
    }
}
