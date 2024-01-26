// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere.Net;

public interface ISendStream
{
    Task<NetResult> Complete();
}

internal class SendStream : ISendStream
{
    public SendStream(SendTransmission sendTransmission, TaskCompletionSource<NetResult> tcs)
    {
        this.sendTransmission = sendTransmission;
        this.tcs = tcs;
    }

    private bool complete;
    private SendTransmission sendTransmission;
    private TaskCompletionSource<NetResult> tcs;

    public Task<NetResult> Complete()
    {
        if (this.complete)
        {
            return Task.FromResult(NetResult.Closed);
        }

        this.sendTransmission.Dispose();
        this.complete = true;

        return this.tcs.Task;
    }
}
