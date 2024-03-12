// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

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
        else
        {
            return this.SendTransmission.ProcessSend(this, buffer, false, cancellationToken);
        }
    }

    public async Task<NetResult> SendBlock<TSend>(TSend data, CancellationToken cancellationToken = default)
    {
        if (!NetHelper.TrySerializeWithLength(data, out var owner))
        {
            return NetResult.SerializationFailed;
        }

        if (owner.Memory.Length > this.SendTransmission.Connection.Agreement.MaxBlockSize)
        {
            return NetResult.BlockSizeLimit;
        }

        NetResult result;
        try
        {
            result = await this.Send(owner.Memory, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            owner.Return();
        }

        return result;
    }

    protected async Task<NetResultValue<TReceive>> InternalComplete<TReceive>(CancellationToken cancellationToken)
    {
        if (this.IsComplete)
        {
            return new(NetResult.Completed);
        }

        var mode = this.SendTransmission.Mode;
        if (mode != NetTransmissionMode.StreamCompleted &&
            mode != NetTransmissionMode.Disposed)
        {
            var result = await this.SendTransmission.ProcessSend(this, ReadOnlyMemory<byte>.Empty, true, cancellationToken).ConfigureAwait(false);
            if (result != NetResult.Success)
            {
                return new(result);
            }
        }

        try
        {
            this.IsComplete = true;

            NetResponse response;
            var connection = this.SendTransmission.Connection;
            var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var receiveTransmission = connection.TryCreateReceiveTransmission(this.SendTransmission.TransmissionId, tcs))/
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
                catch
                {
                    return new(NetResult.Canceled);
                }
            }

            if (typeof(TReceive) == typeof(NetResult))
            {// In the current implementation, the value of NetResult is assigned to DataId.
                response.Return();
                var netResult = (NetResult)response.DataId;
                return new(NetResult.Success, Unsafe.As<NetResult, TReceive>(ref netResult));
            }

            if (!NetHelper.TryDeserialize<TReceive>(response.Received, out var receive))
            {
                response.Return();
                return new(NetResult.DeserializationFailed);
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
