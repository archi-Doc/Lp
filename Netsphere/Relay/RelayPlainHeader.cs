// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Netsphere.Relay;

#pragma warning disable CS0649

[StructLayout(LayoutKind.Explicit)]
public readonly struct RelayPlainHeader
{// 8 bytes, RelayHeaderCode
    public const int RelayIdLength = 4; // SourceRelayId/DestinationRelayId
    public const int Length = 8;

    public RelayHeader(uint salt)
    {
        this.Salt = salt;
        this.NetAddress = netAddress;
    }

    [FieldOffset(0)]
    public readonly uint Salt; // 4 bytes
    [FieldOffset(4)]
    public readonly uint Zero; // 4 bytes. The byte sequence starting from zero is subject to encryption.
    [FieldOffset(8)]
    public readonly NetAddress NetAddress; // 24 bytes
}
