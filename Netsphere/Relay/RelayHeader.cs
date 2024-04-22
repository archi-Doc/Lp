// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

#pragma warning disable CS0649

public readonly struct RelayHeader
{// 32 bytes, RelayHeaderCode
    public const int Length = 32;

    public RelayHeader(uint salt, NetAddress netAddress)
    {
        this.Salt = salt;
        this.NetAddress = netAddress;
    }

    public readonly uint Zero; // 4 bytes
    public readonly uint Salt; // 4 bytes
    public readonly NetAddress NetAddress; // 24 bytes
}
