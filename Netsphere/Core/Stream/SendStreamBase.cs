// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

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

    public ulong DataId { get; protected set; }

    public long RemainingLength { get; internal set; }

    public long SentLength { get; internal set; }

    internal void DisposeImmediately()
    {
        if (this.SendTransmission.Mode == NetTransmissionMode.Stream)
        {

        }

        this.SendTransmission.Dispose();
    }

    public async Task Cancel(CancellationToken cancellationToken = default)
    {
        var result = await this.SendTransmission.ProcessSend(this, DataControl.Cancel, ReadOnlyMemory<byte>.Empty, cancellationToken).ConfigureAwait(false);
        this.SendTransmission.Dispose();
    }

    public async Task<NetResult> Send(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        /* if (!this.SendTransmission.Connection.IsActive)
        {// -> this.SendTransmission.ProcessSend()
            return NetResult.Closed;
        }*/

        if (this.SendTransmission.Mode != NetTransmissionMode.Stream)
        {
            return NetResult.InvalidOperation;
        }

        var result = await this.SendTransmission.ProcessSend(this, DataControl.Valid, buffer, cancellationToken).ConfigureAwait(false);
        if (result != NetResult.Success &&
            result != NetResult.Completed)
        {//
            this.DisposeImmediately();
        }

        return result;
    }

    public async Task<NetResult> SendBlock<TSend>(TSend data, CancellationToken cancellationToken = default)
    {
        /* if (!this.SendTransmission.Connection.IsActive)
        {// -> this.SendTransmission.ProcessSend()
            return NetResult.Closed;
        }

        if (this.SendTransmission.Mode != NetTransmissionMode.Stream)
        {// -> this.Send()
            return NetResult.InvalidOperation;
        }*/

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

    protected async Task<NetResult> SendControl(DataControl transmissionControl, CancellationToken cancellationToken)
    {
        /* if (!this.SendTransmission.Connection.IsActive)
        {// -> this.SendTransmission.ProcessSend()
            return NetResult.Closed;
        }*/

        if (this.SendTransmission.Mode != NetTransmissionMode.Stream)
        {
            return NetResult.InvalidOperation;
        }

        var result = await this.SendTransmission.ProcessSend(this, transmissionControl, ReadOnlyMemory<byte>.Empty, cancellationToken).ConfigureAwait(false);
        return result;
    }

    protected async Task<NetResultValue<TReceive>> InternalComplete<TReceive>(DataControl dataControl, CancellationToken cancellationToken)
    {
        if (!this.SendTransmission.Connection.IsActive)
        {
            return new(NetResult.Closed);
        }

        if (this.SendTransmission.Mode != NetTransmissionMode.Stream)
        {
            return new(NetResult.InvalidOperation);
        }

        this.SendTransmission.Connection.SendStreamFrame(this.SendTransmission.TransmissionId, Packet.StreamFrameType.Complete);
        this.SendTransmission.Mode = NetTransmissionMode.StreamCompleted;

        /*var mode = this.SendTransmission.Mode;
        if (mode != NetTransmissionMode.StreamCompleted &&
            mode != NetTransmissionMode.Disposed)
        {
            var result = await this.SendTransmission.ProcessSend(this, ReadOnlyMemory<byte>.Empty, true, cancellationToken).ConfigureAwait(false);
            if (result != NetResult.Success)
            {
                return new(result);
            }
        }*/

        try
        {
            this.IsComplete = true;

            var connection = this.SendTransmission.Connection;
            if (connection.IsServer)
            {// On the server side, it does not receive completion of the stream since ReceiveTransmission is already consumed.
                var result = NetResult.Success;
                if (this.SendTransmission.SentTcs is { } sentTcs)
                {//
                    result = await sentTcs.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                }

                return new(result);
            }

            NetResponse response;
            var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var receiveTransmission = connection.TryCreateReceiveTransmission(this.SendTransmission.TransmissionId, tcs))
            {
                if (receiveTransmission is null)
                {
                    return new(NetResult.NoTransmission);
                }

                try
                {//
                    response = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
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
