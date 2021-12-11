// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Netsphere;

// PacketId.Data (First)
// DataHeader
// byte[DataSize] Data

// PacketId.Data2 (Second and later)
// DataHeader2
// byte[DataSize] Data

[StructLayout(LayoutKind.Explicit)]
internal readonly struct DataHeader
{
    public static ulong ChecksumMask = 0xffffffffffffff00ul;

    public DataHeader(ulong dataId, PacketId packetId, ulong checksum)
    {
        this.DataId = dataId;
        this.PacketId = packetId;
        this.Checksum = (checksum & ChecksumMask) | (byte)packetId;
    }

    public bool ChecksumEquals(ulong checksum)
        => (checksum & ChecksumMask) == (this.Checksum & ChecksumMask);

    [FieldOffset(0)]
    public readonly ulong DataId;

    [FieldOffset(8)]
    public readonly PacketId PacketId;

    [FieldOffset(8)]
    public readonly ulong Checksum; // 1-7bytes, FarmHash64
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct DataHeader2
{
    public DataHeader2(ulong checksum)
    {
        this.Checksum = checksum & DataHeader.ChecksumMask;
    }

    [FieldOffset(0)]
    public readonly ulong Checksum; // 1-7bytes, FarmHash64
}
