// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    // private readonly object syncObject = new();
    private NetGene? sendGene; // Single gene
    private NetGene.GoshujinClass? sendGenes; // Multiple genes

    #endregion

    internal async Task<NetResult> SendBlockAsync(uint blockType, ulong blockId, ByteArrayPool.MemoryOwner block)
    {
        if (this.sendGene is not null || this.sendGenes is not null)
        {
            return NetResult.TransmissionConsumed;
        }

        var size = sizeof(uint) + sizeof(ulong) + block.Span.Length;
        var blockInfo = CalculateBlock(size);

        return NetResult.Success;

        /*if (blockInfo.NumberOfBlocks == 1)
        {
            this.sendGene = new(0, 1);
            this.sendGene.SetSend(this.Connection, this.TransmissionId, 0, 1, block);
        }
        else
        {

        }

        this.sendGene*/
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int NumberOfBlocks, int LastBlockSize) CalculateBlock(int size)
    {
        var numberOfBLocks = size / GeneFrame.MaxBlockLength;
        var lastBlockSize = size - (numberOfBLocks * GeneFrame.MaxBlockLength);
        return (lastBlockSize > 0 ? numberOfBLocks + 1 : numberOfBLocks, lastBlockSize);
    }
}
