// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;

namespace Netsphere;

/// <summary>
/// Represents a server invocation.
/// </summary>
public readonly record struct ServerInvocationParam
{
    public ServerInvocationParam(Connection connection, ReceiveTransmission receiveTransmission, uint primaryId, ulong secondaryId, ByteArrayPool.MemoryOwner owner)
    {
        this.Connection = connection;
        this.ReceiveTransmission = receiveTransmission;
        this.PrimaryId = primaryId;
        this.SecondaryId = secondaryId;
        this.Owner = owner;
    }

    public void Return() => this.Owner.Return();

    public readonly Connection Connection;
    public readonly ReceiveTransmission ReceiveTransmission;
    public readonly uint PrimaryId;
    public readonly ulong SecondaryId;
    public readonly ByteArrayPool.MemoryOwner Owner;
}
