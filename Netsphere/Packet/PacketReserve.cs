// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketReserve : IPacket
{
    public const int MaxGenes = 4096;

    public PacketId PacketId => PacketId.Reserve;

    public PacketReserve()
    {
    }

    public PacketReserve(int totalSize)
    {
        var info = PacketService.GetDataInfo(totalSize);
        this.NumberOfGenes = (ushort)info.NumberOfGenes;
        this.DataSize = (uint)info.DataSize;
    }

    // public bool Response { get; set; }

    /// <summary>
    /// Gets or sets the number of genes used for data transfer.
    /// </summary>
    [Key(0)]
    public ushort NumberOfGenes { get; set; }

    /// <summary>
    /// Gets or sets the size of the total data in bytes.
    /// </summary>
    [Key(1)]
    public uint DataSize { get; set; }
}

[TinyhandObject]
internal partial class PacketReserveResponse : IPacket
{
    public PacketId PacketId => PacketId.ReserveResponse;

    public PacketReserveResponse()
    {
    }
}
