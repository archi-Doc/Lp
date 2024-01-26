// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
        Debug.Assert(sendTransmission.Mode == NetTransmissionMode.Stream);

        this.sendTransmission = sendTransmission;
    }

    private bool complete;
    private SendTransmission sendTransmission;

    public Task<NetResult> Send(ReadOnlyMemory<byte> buffer)
        => this.sendTransmission.ProcessSend(buffer);

    public async Task<NetResult> Complete()
    {
        if (this.complete)
        {
            return NetResult.Closed;
        }

        var result = NetResult.Success;
        if (this.sendTransmission.SentTcs is { } tcs)
        {
            result = await tcs.Task.ConfigureAwait(false);
        }

        this.sendTransmission.Dispose();
        this.complete = true;

        return result;
    }
}
