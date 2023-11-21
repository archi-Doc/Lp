// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Netsphere;

[StructLayout(LayoutKind.Explicit)]
internal struct GeneHeader
{// 16 bytes
    [FieldOffset(0)]
    public PacketId Id;

    [FieldOffset(2)]
    public ushort DataSize;

    [FieldOffset(8)]
    public ulong Gene;
}
