// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere.Server;

public class TransmissionContext
{
    public TransmissionContext(ConnectionContext connectionContext, uint transmissionId, uint dataKind, ulong dataId, ByteArrayPool.MemoryOwner? owner)
    {
        this.ConnectionContext = connectionContext;
        this.TransmissionId = transmissionId;
        this.DataKind = dataKind;
        this.DataId = dataId;
        this.Owner = owner;
    }

    public ConnectionContext ConnectionContext { get; }

    public ServerConnection Connection => this.ConnectionContext.ServerConnection;

    public uint TransmissionId { get; }

    public uint DataKind { get; }

    public ulong DataId { get; }

    public ByteArrayPool.MemoryOwner? Owner { get; set; }

    public void Return()
        => this.Owner = this.Owner?.Return();

    public NetResult SendAndForget<TSend>(TSend packet, ulong dataId = 0)
        where TSend : ITinyhandSerialize<TSend>
    {
        if (this.Connection.NetBase.CancellationToken.IsCancellationRequested)
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
        return result;
    }
}
