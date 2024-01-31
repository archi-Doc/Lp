// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;
using Netsphere.Net;

#pragma warning disable SA1401

namespace Netsphere.Server;

public sealed class TransmissionContext
{
    public static TransmissionContext Current => AsyncLocal.Value!;

    internal static AsyncLocal<TransmissionContext?> AsyncLocal = new();

    public TransmissionContext(ConnectionContext connectionContext, uint transmissionId, uint dataKind, ulong dataId, ByteArrayPool.MemoryOwner toBeShared)
    {
        this.ConnectionContext = connectionContext;
        this.TransmissionId = transmissionId;
        this.DataKind = dataKind;
        this.DataId = dataId;
        this.Owner = toBeShared;
    }

    #region FieldAndProperty

    public ConnectionContext ConnectionContext { get; }

    public ServerConnection Connection => this.ConnectionContext.ServerConnection;

    public uint TransmissionId { get; }

    public uint DataKind { get; }

    public ulong DataId { get; }

    public ByteArrayPool.MemoryOwner Owner { get; set; }

    public NetResult Result { get; set; }

    private bool sent;

    #endregion

    public void Return()
        => this.Owner = this.Owner.Return();

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
        else if (this.sent)
        {
            return NetResult.AlreadySent;
        }

        var transmission = this.Connection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            return NetResult.NoTransmission;
        }

        this.sent = true;
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
        else if (this.sent)
        {
            return NetResult.AlreadySent;
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

        this.sent = true;
        var result = transmission.SendBlock(0, dataId, owner, default);
        owner.Return();
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }

    public async Task<(NetResult Result, SendStream? Stream)> SendStream(long maxLength, ulong dataId = 0)
    {
        if (this.Connection.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Canceled, default);
        }
        else if (this.Connection.Agreement.MaxStreamLength < maxLength)
        {
            return (NetResult.StreamLengthLimit, default);
        }
        else if (this.sent)
        {
            return (NetResult.AlreadySent, default);
        }

        var timeout = this.Connection.NetBase.DefaultSendTimeout;
        var transmission = this.Connection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            return (NetResult.NoTransmission, default);
        }

        var tcs = new TaskCompletionSource<NetResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.sent = true;
        var result = transmission.SendStream(maxLength, tcs);
        if (result != NetResult.Success)
        {
            transmission.Dispose();
            return (result, default);
        }

        return (NetResult.Success, new SendStream(transmission, maxLength, dataId));
    }
}
