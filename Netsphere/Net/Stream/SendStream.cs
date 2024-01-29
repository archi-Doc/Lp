// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

public interface ISendStream
{
    Task<NetResult> Send(ReadOnlyMemory<byte> buffer);

    Task<NetResult> Complete();
}

public class SendStream : ISendStream
{
    internal SendStream(SendTransmission sendTransmission, ulong dataId)
    {
        this.sendTransmission = sendTransmission;
        this.DataId = dataId;
    }

    #region FieldAndProperty

    public ulong DataId { get; }

    private readonly SendTransmission sendTransmission;
    private bool isComplete;

    public bool IsComplete
        => this.isComplete;

    #endregion

    public Task<NetResult> Send(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (this.isComplete)
        {
            return Task.FromResult(NetResult.Completed);
        }
        else if (buffer.Length == 0)
        {
            return Task.FromResult(NetResult.Success);
        }
        else
        {
            return this.sendTransmission.ProcessSend(buffer, this.DataId, cancellationToken);
        }
    }

    public async Task<NetResult> Complete(CancellationToken cancellationToken = default)
    {
        if (this.isComplete)
        {
            return NetResult.Completed;
        }

        await this.sendTransmission.ProcessSend(ReadOnlyMemory<byte>.Empty, this.DataId, cancellationToken);

        var result = NetResult.Success;
        if (this.sendTransmission.SentTcs is { } tcs)
        {
            try
            {
                result = await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
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
