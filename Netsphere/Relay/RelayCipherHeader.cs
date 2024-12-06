// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Netsphere.Relay;

#pragma warning disable CS0649

[StructLayout(LayoutKind.Explicit)]
public readonly struct RelayCipherHeader
{// 28 bytes, RelayHeaderCode
    public const int Length = 28;

    public RelayCipherHeader(NetAddress netAddress)
    {
        this.NetAddress = netAddress;
    }

    [FieldOffset(0)]
    public readonly uint Zero; // 4 bytes.
    [FieldOffset(4)]
    public readonly NetAddress NetAddress; // 24 bytes
}
