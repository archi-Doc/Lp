// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketData : IPacket
{
    public PacketId Id => PacketId.Data;

    [Key(0)]
    public bool Response { get; set; }

    /// <summary>
    /// Gets or sets the number of packets used for data transfer.
    /// 0: Sequential, n: Blast.
    /// </summary>
    [Key(1)]
    public ushort NumberOfPackets { get; set; }

    [Key(2)]
    public uint DataSize { get; set; }

    [Key(3)]
    public ulong Checksum { get; set; }

    // byte[DataSize] Data
}
