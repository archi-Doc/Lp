// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketReserve : IPacket
{
    public const int MaxGenes = 4096;

    public PacketId Id => PacketId.Reserve;

    // public bool Response { get; set; }

    /// <summary>
    /// Gets or sets the number of genes used for data transfer.
    /// </summary>
    [Key(0)]
    public ushort NumberOfGenes { get; set; }

    [Key(1)]
    public uint DataSize { get; set; }
}
