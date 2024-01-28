// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;

namespace Netsphere.Net;

public interface ISendStream
{
    Task<NetResult> Send(ReadOnlyMemory<byte> buffer);

    Task<NetResult> Complete();
}

internal class SendStream : ISendStream
{
    public SendStream(SendTransmission sendTransmission)
    {
        this.sendTransmission = sendTransmission;
    }

    private readonly SendTransmission sendTransmission;
    private bool isComplete;

    public bool IsComplete
        => this.isComplete;

    public Task<NetResult> Send(ReadOnlyMemory<byte> buffer)
        => buffer.Length == 0 ? Task.FromResult(NetResult.Success) : this.sendTransmission.ProcessSend(buffer);

    public async Task<NetResult> Complete()
    {
        if (this.isComplete)
        {
            return NetResult.Completed;
        }

        await this.sendTransmission.ProcessSend(ReadOnlyMemory<byte>.Empty);

        var result = NetResult.Success;
        if (this.sendTransmission.SentTcs is { } tcs)
        {
            try
            {
                result = await tcs.Task.WaitAsync(this.sendTransmission.Connection.CancellationToken).ConfigureAwait(false);
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

        this.sendTransmission.Dispose();
        this.isComplete = true;

        return result;
    }
}
