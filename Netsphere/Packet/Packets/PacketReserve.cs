// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketReserveObsolete : IPacketObsolete
{
    public const int MaxGenes = 4096;

    public PacketIdObsolete PacketId => PacketIdObsolete.Reserve;

    public PacketReserveObsolete()
    {
    }

    public PacketReserveObsolete(int totalSize)
    {
        var info = PacketService.GetDataSize(totalSize);
        this.NumberOfGenes = (ushort)info.NumberOfGenes;
        this.TotalSize = totalSize;
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
    public int TotalSize { get; set; }
}

[TinyhandObject]
internal partial class PacketReserveResponseObsolete : IPacketObsolete
{
    public PacketIdObsolete PacketId => PacketIdObsolete.ReserveResponse;

    public PacketReserveResponseObsolete()
    {
    }
}
