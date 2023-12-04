// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Netsphere.Packet;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class SendTransmission
{
    public enum TransmissionMode
    {
        SendAndForget,
        SendAndReceive,
        ReceiveOnly,
        ReceiveAndSend,
    }

    public enum TransmissionState
    {
        Initial,
        Sending,
        Receiving,
    }

    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "TransmissionId", AddValue = false, Accessibility = ValueLinkAccessibility.Private)]
    [Link(Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false, Accessibility = ValueLinkAccessibility.Private)]
    public SendTransmission(Connection connection, uint transmissionId)
    {
        this.Connection = connection;
        this.TransmissionId = transmissionId;
    }

    #region FieldAndProperty

    public Connection Connection { get; }

    public uint TransmissionId { get; }

    public TransmissionState State
    {
        get
        {
            if (this.sendGene is not null ||
                this.sendGenes is not null)
            {
                return TransmissionState.Sending;
            }
            else if (this.recvGene is not null ||
                this.recvGenes is not null)
            {
                return TransmissionState.Receiving;
            }
            else
            {
                return TransmissionState.Initial;
            }
        }
    }

    private readonly object syncObject = new();
    private NetGene? sendGene; // Single gene
    private NetGene.GoshujinClass? sendGenes; // Multiple genes
    private NetGene? recvGene; // Single gene
    private NetGene.GoshujinClass? recvGenes; // Multiple genes

    #endregion

    internal NetResult SendBlock(uint blockType, ulong blockId, ByteArrayPool.MemoryOwner block)
    {
        if (this.sendGene is not null || this.sendGenes is not null)
        {
            return NetResult.TransmissionConsumed;
        }

        var size = sizeof(uint) + sizeof(ulong) + block.Span.Length;
        var info = CalculateGene(size);

        lock (this.syncObject)
        {
            if (info.NumberOfGenes == 1)
            {// Single gene
                this.sendGene = new();
                if (this.CreatePacket(info.NumberOfGenes, this.sendGene, block.Span, out var owner))
                {
                    this.sendGene.SetSend(owner);
                }
            }
            else
            {// Multiple genes
            }
        }

        if (info.NumberOfGenes > FlowTerminal.GeneThreshold)
        {// Flow control
        }
        else
        {
            this.Connection.ConnectionTerminal.AddSend(this);
        }

        return NetResult.Success;
    }

    internal async Task<NetResponse> ReceiveBlock()
    {
        return new();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int NumberOfGenes, int LastGeneSize) CalculateGene(int size)
    {
        var numberOfGenes = size / GeneFrame.MaxBlockLength;
        var lastGeneSize = size - (numberOfGenes * GeneFrame.MaxBlockLength);
        return (lastGeneSize > 0 ? numberOfGenes + 1 : numberOfGenes, lastGeneSize);
    }

    private bool CreatePacket(int geneTotal, NetGene gene, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
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
