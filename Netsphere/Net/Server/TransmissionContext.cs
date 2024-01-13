// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

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

    public ConnectionContext ConnectionContext { get; }

    public ServerConnection Connection => this.ConnectionContext.ServerConnection;

    public uint TransmissionId { get; }

    public uint DataKind { get; }

    public ulong DataId { get; }

    public ByteArrayPool.MemoryOwner Owner { get; set; }

    public NetResult Result { get; set; }

    public void Return()
        => this.Owner = this.Owner.Return();

    public NetResult SendAndForget(ByteArrayPool.MemoryOwner toBeShared, ulong dataId = 0)
    {
        if (this.Connection.IsClosedOrDisposed)
        {
            return NetResult.Closed;
        }

        if (this.Connection.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        var transmission = this.Connection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            return NetResult.NoTransmission;
        }

        var result = transmission.SendBlock(0, dataId, toBeShared, default);
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }

    public NetResult SendAndForget<TSend>(TSend packet, ulong dataId = 0)
    {
        if (this.Connection.IsClosedOrDisposed)
        {
            return NetResult.Closed;
        }

        if (this.Connection.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return NetResult.SerializationError;
        }

        var transmission = this.Connection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            owner.Return();
            return NetResult.NoTransmission;
        }

        var result = transmission.SendBlock(0, dataId, owner, default);
        owner.Return();
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }
}
