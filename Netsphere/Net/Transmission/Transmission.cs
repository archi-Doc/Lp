// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Netsphere.Packet;

namespace Netsphere.Transmission;

public abstract class Transmission
{
    public Transmission(Connection connection, uint transmissionId)
    {
        this.Connection = connection;
        this.TransmissionId = transmissionId;
    }

    #region FieldAndProperty

    public Connection Connection { get; }

    public uint TransmissionId { get; }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int NumberOfBlocks, int LastBlockSize) CalculateBlock(int size)
    {
        var numberOfBLocks = size / GeneFrame.MaxBlockLength;
        var lastBlockSize = size - (numberOfBLocks * GeneFrame.MaxBlockLength);
        return (lastBlockSize > 0 ? numberOfBLocks + 1 : numberOfBLocks, lastBlockSize);
    }

    internal bool CreatePacket(int geneTotal, NetGene gene, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert(block.Length <= GeneFrame.MaxBlockLength);

        // GeneFrameeCode
        Span<byte> frameHeader = stackalloc byte[GeneFrame.Length];
        var span = frameHeader;

        BitConverter.TryWriteBytes(span, (ushort)FrameType.Block); // Frame type
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.TransmissionId); // TransmissionId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, gene.GeneSerial); // GeneSerial
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, geneTotal); // GeneMax
        span = span.Slice(sizeof(uint));

        return this.Connection.CreatePacket(frameHeader, block, out owner);
    }
}
