// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;
using Netsphere.Net;

#pragma warning disable SA1401

namespace Netsphere.Server;

public sealed class TransmissionContext
{
    public static TransmissionContext Current => AsyncLocal.Value!;

    internal static AsyncLocal<TransmissionContext?> AsyncLocal = new();

    internal TransmissionContext(ServerConnection connection, uint transmissionId, uint dataKind, ulong dataId, ByteArrayPool.MemoryOwner toBeShared)
    {
        this.Connection = connection;
        this.TransmissionId = transmissionId;
        this.DataKind = dataKind;
        this.DataId = dataId;
        this.Owner = toBeShared;
    }

    #region FieldAndProperty

    // public ServerConnectionContext ConnectionContext { get; }

    public ServerConnection Connection { get; } // => this.ConnectionContext.ServerConnection;

    public uint TransmissionId { get; }

    public uint DataKind { get; }

    public ulong DataId { get; }

    public ByteArrayPool.MemoryOwner Owner { get; set; }

    public NetResult Result { get; set; }

    public bool IsSent { get; private set; }

    public ReceiveStream ReceiveStream
        => this.receiveStream ?? throw new InvalidOperationException();

    private ReceiveStream? receiveStream;

    #endregion

    public void Return()
    {
        this.Owner = this.Owner.Return();
        if (this.receiveStream is not null)
        {
            this.receiveStream.Abort();
            this.receiveStream = default;
        }
    }

    public NetResult SendAndForget(ByteArrayPool.MemoryOwner toBeShared, ulong dataId = 0)
    {
        if (this.Connection.IsClosedOrDisposed)
        {
            return NetResult.Closed;
        }
        else if (this.Connection.CancellationToken.IsCancellationRequested)
        {
            return default;
        }
        else if (this.IsSent)
        {
            return NetResult.InvalidOperation;
        }

        var transmission = this.Connection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            return NetResult.NoTransmission;
        }

        this.IsSent = true;
        var result = transmission.SendBlock(0, dataId, toBeShared, default);
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }

    public NetResult SendAndForget<TSend>(TSend data, ulong dataId = 0)
    {
        if (this.Connection.IsClosedOrDisposed)
        {
            return NetResult.Closed;
        }
        else if (this.Connection.CancellationToken.IsCancellationRequested)
        {
            return default;
        }
        else if (this.IsSent)
        {
            return NetResult.InvalidOperation;
        }

        if (!BlockService.TrySerialize(data, out var owner))
        {
            return NetResult.SerializationError;
        }

        var transmission = this.Connection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            owner.Return();
            return NetResult.NoTransmission;
        }

        this.IsSent = true;
        var result = transmission.SendBlock(0, dataId, owner, default);
        owner.Return();
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }

    public (NetResult Result, SendStream? Stream) SendStream(long maxLength, ulong dataId = 0)
    {
        if (this.Connection.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Canceled, default);
        }
        else if (!this.Connection.Agreement.CheckStreamLength(maxLength))
        {
            return (NetResult.StreamLengthLimit, default);
        }
        else if (this.IsSent)
        {
            return (NetResult.InvalidOperation, default);
        }

        var transmission = this.Connection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            return (NetResult.NoTransmission, default);
        }

        var tcs = new TaskCompletionSource<NetResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.IsSent = true;
        var result = transmission.SendStream(maxLength, tcs);
        if (result != NetResult.Success)
        {
            transmission.Dispose();
            return (result, default);
        }

        return (NetResult.Success, new SendStream(transmission, maxLength, dataId));
    }

    /*public (NetResult Result, ReceiveStream? Stream) ReceiveStream(long maxLength)
    {
        if (this.Connection.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Canceled, default);
        }
        else if (!this.Connection.Agreement.CheckStreamLength(maxLength))
        {
            return (NetResult.StreamLengthLimit, default);
        }
        else if (this.receiveTransmission is null)
        {
            return (NetResult.InvalidOperation, default);
        }

        var stream = new ReceiveStream(this.receiveTransmission, this.DataId, maxLength);
        this.receiveTransmission = default;
        return (NetResult.Success, stream);
    }*/

    internal bool CreateReceiveStream(ReceiveTransmission receiveTransmission, long maxLength)
    {
        if (this.Connection.CancellationToken.IsCancellationRequested)
        {
            return false;
        }
        else if (!this.Connection.Agreement.CheckStreamLength(maxLength))
        {
            return false;
        }
        else if (this.receiveStream is not null)
        {
            return false;
        }

        this.receiveStream = new ReceiveStream(receiveTransmission, this.DataId, maxLength);
        return true;
    }

    /*internal NetResult ForceSendAndForget(ByteArrayPool.MemoryOwner toBeShared, ulong dataId = 0)
    {
        var transmission = this.Connection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            return NetResult.NoTransmission;
        }

        this.IsSent = true;
        var result = transmission.SendBlock(0, dataId, toBeShared, default);
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }*/
}
