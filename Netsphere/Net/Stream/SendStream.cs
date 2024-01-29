// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

#pragma warning disable SA1202 // Elements should be ordered by access

public abstract class SendStreamBase
{
    internal SendStreamBase(SendTransmission sendTransmission, long maxLength, ulong dataId)
    {
        this.SendTransmission = sendTransmission;
        this.RemainingLength = maxLength;
        this.DataId = dataId;
    }

    internal SendTransmission SendTransmission { get; }

    public bool IsComplete { get; protected set; }

    public ulong DataId { get; protected set; }

    public long RemainingLength { get; internal set; }

    public long SentLength { get; internal set; }

    public Task<NetResult> Send(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (this.IsComplete)
        {
            return Task.FromResult(NetResult.Completed);
        }
        else if (buffer.Length == 0)
        {
            return Task.FromResult(NetResult.Success);
        }
        else
        {
            return this.SendTransmission.ProcessSend(this, buffer, cancellationToken);
        }
    }
}

public class SendStream : SendStreamBase
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
