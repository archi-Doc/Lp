// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Netsphere;

[StructLayout(LayoutKind.Explicit)]
internal struct PacketHeader
{// 16 bytes
    [FieldOffset(0)]
    public ushort Engagement;

    [FieldOffset(2)]
    public byte Cage;

    [FieldOffset(3)]
    public PacketId Id;

    [FieldOffset(4)]
    public ushort DataSize;

    [FieldOffset(8)]
    public ulong Gene;
}
