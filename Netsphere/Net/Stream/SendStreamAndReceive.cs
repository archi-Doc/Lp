// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere.Net;

public interface ISendStream<TReceive>
{
    Task<NetResultValue<TReceive>> CompleteAndReceive();
}

internal class SendStreamAndReceive<TReceive> : ISendStream<TReceive>
{
    public SendStreamAndReceive(SendTransmission sendTransmission)
    {
        this.sendTransmission = sendTransmission;
    }

    private bool complete;
    private SendTransmission sendTransmission;

    public async Task<NetResultValue<TReceive>> CompleteAndReceive()
    {
        if (this.complete)
        {
            return new(NetResult.Closed);
        }

        var connection = this.sendTransmission.Connection;
        var transmissionId = this.sendTransmission.TransmissionId;

        this.sendTransmission.Dispose();
        this.complete = true;

        NetResponse response;
        var timeout = connection.NetBase.DefaultSendTimeout;
        var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var receiveTransmission = connection.TryCreateReceiveTransmission(transmissionId, tcs, default))
        {
            if (receiveTransmission is null)
            {
                return new(NetResult.NoTransmission);
            }

            try
            {
                response = await tcs.Task.WaitAsync(timeout, connection.CancellationToken).ConfigureAwait(false);
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

        if (!BlockService.TryDeserialize<TReceive>(response.Received, out var receive))
        {
            response.Return();
            return new(NetResult.DeserializationError);
        }

        response.Return();
        return new(NetResult.Success, receive);
    }
}
