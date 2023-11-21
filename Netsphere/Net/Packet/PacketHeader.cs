// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Netsphere;

[StructLayout(LayoutKind.Explicit)]
internal struct PacketHeader
{// 8 bytes
    [FieldOffset(0)]
    public uint PacketId;

    [FieldOffset(4)]
    public PacketType PacketType;

    [FieldOffset(6)]
    public ushort DataSize;
}
