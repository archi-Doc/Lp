// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Server;

public class TransmissionContext
{
    public TransmissionContext(ConnectionContext connectionContext, uint dataKind, ulong dataId, ByteArrayPool.MemoryOwner? owner)
    {
        this.ConnectionContext = connectionContext;
        this.DataKind = dataKind;
        this.DataId = dataId;
        this.Owner = owner;
    }

    public ConnectionContext ConnectionContext { get; }

    public uint DataKind { get; }

    public ulong DataId { get; }

    public ByteArrayPool.MemoryOwner? Owner { get; set; }
}
