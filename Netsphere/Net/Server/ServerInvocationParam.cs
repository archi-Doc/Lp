// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;

namespace Netsphere;

/// <summary>
/// Represents a server invocation.
/// </summary>
public readonly record struct ServerInvocationParam
{
    public ServerInvocationParam(Connection connection, uint dataKind, ulong dataId, ByteArrayPool.MemoryOwner owner, ISendTransmission sendTransmission, SendStream? sendStream)
    {
        this.Connection = connection;
        this.DataKind = dataKind;
        this.DataId = dataId;
        this.Owner = owner;
        this.SendTransmission = sendTransmission;
        this.SendStream = sendStream;
    }

    public void Return() => this.Owner.Return();

    public readonly Connection Connection;
    public readonly uint DataKind;
    public readonly ulong DataId;
    public readonly ByteArrayPool.MemoryOwner Owner;
    public readonly ISendTransmission SendTransmission;
    public readonly SendStream? SendStream;
}
