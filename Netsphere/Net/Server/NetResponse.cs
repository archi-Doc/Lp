// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;

namespace Netsphere;

/// <summary>
/// Represents a server invocation.
/// </summary>
public readonly record struct ServerInvocationParam
{
    public ServerInvocationParam(Connection connection, ReceiveTransmission receiveTransmission, uint dataKind, ulong dataId, ByteArrayPool.MemoryOwner owner)
    {
        this.Connection = connection;
        this.ReceiveTransmission = receiveTransmission;
        this.DataKind = dataKind;
        this.DataId = dataId;
        this.Owner = owner;
    }

    public void Return() => this.Owner.Return();

    public readonly Connection Connection;
    public readonly ReceiveTransmission ReceiveTransmission;
    public readonly uint DataKind;
    public readonly ulong DataId;
    public readonly ByteArrayPool.MemoryOwner Owner;
}
