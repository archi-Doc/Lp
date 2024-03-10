// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;

namespace Netsphere;

public class SendStreamAndReceive<TReceive> : SendStreamBase
{
    internal SendStreamAndReceive(SendTransmission sendTransmission, long maxLength, ulong dataId)
        : base(sendTransmission, maxLength, dataId)
    {
    }

    public async Task<NetResultValue<TReceive>> CompleteAndReceive(CancellationToken cancellationToken = default)
    {
        if (this.IsComplete)
        {
            return new(NetResult.Completed);
        }

        if (this.SendTransmission.Mode != NetTransmissionMode.StreamCompleted)
        {
            await this.SendTransmission.ProcessSend(this, ReadOnlyMemory<byte>.Empty, cancellationToken);
        }

        try
        {
            this.IsComplete = true;

            NetResponse response;
            var connection = this.SendTransmission.Connection;
            // var timeout = connection.NetBase.DefaultSendTimeout;
            var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var receiveTransmission = connection.TryCreateReceiveTransmission(this.SendTransmission.TransmissionId, tcs))
            {
                if (receiveTransmission is null)
                {
                    return new(NetResult.NoTransmission);
                }

                try
                {
                    response = await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
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

            if (!NetHelper.TryDeserialize<TReceive>(response.Received, out var receive))
            {//
                response.Return();
                return new(NetResult.DeserializationError);
            }

            response.Return();
            return new(NetResult.Success, receive);
        }
        finally
        {
            this.SendTransmission.Dispose();
        }
    }
}
