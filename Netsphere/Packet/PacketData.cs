// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Netsphere;

[TinyhandObject]
internal partial class PacketData : IPacket
{
    public PacketId Id => PacketId.Data;

    // DataHeader
    // byte[DataSize] Data
}

[StructLayout(LayoutKind.Explicit)]
internal struct DataHeader
{
    [FieldOffset(0)]
    public PacketId PacketId;

    [FieldOffset(4)]
    public uint Id;

    [FieldOffset(8)]
    public ulong Checksum; // FarmHash64
}
