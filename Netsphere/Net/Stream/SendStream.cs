// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;

namespace Netsphere;

public class SendStream : SendStreamBase, StreamService
{
    internal SendStream(SendTransmission sendTransmission, long maxLength, ulong dataId)
        : base(sendTransmission, maxLength, dataId)
    {
    }

    public async Task<NetResult> Complete(CancellationToken cancellationToken = default)
    {
        if (this.IsComplete)
        {
            return NetResult.Completed;
        }

        await this.SendTransmission.ProcessSend(this, ReadOnlyMemory<byte>.Empty, cancellationToken);

        var result = NetResult.Success;
        if (this.SendTransmission.SentTcs is { } tcs)
        {
            try
            {
                result = await tcs.Task.WaitAsync(this.SendTransmission.Connection.CancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return NetResult.Timeout;
            }
            catch
            {
                return NetResult.Canceled;
            }
        }

        this.SendTransmission.Dispose();
        this.IsComplete = true;

        return result;
    }
}
