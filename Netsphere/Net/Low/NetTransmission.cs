// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Netsphere.Packet;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class NetTransmission // : IDisposable
{
    internal const int GeneThreshold = 3;

    public enum TransmissionState
    {
        Initial,
        Sending,
        Receiving,
        Complete,
        Disposed,
    }

    [Link(Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false)]
    [Link(Name = "ResendQueue", Type = ChainType.QueueList, AutoLink = false)]
    public NetTransmission(Connection connection, uint transmissionId)
    {
        this.Connection = connection;
        this.TransmissionId = transmissionId;
    }

    #region FieldAndProperty

    public Connection Connection { get; }

    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint TransmissionId { get; }

    public TransmissionState State { get; private set; } // lock (this.syncObject)

    private readonly object syncObject = new();
    private TaskCompletionSource<NetResult>? tcs;
    private NetGene? gene0; // Gene 0
    private NetGene? gene1; // Gene 1
    private NetGene? gene2; // Gene 2
    private NetGene.GoshujinClass? genes; // Multiple genes

    #endregion

    public void Dispose()
    {
        TaskCompletionSource<NetResult>? tcs;

        lock (this.syncObject)
        {
            if (this.State == TransmissionState.Disposed)
            {
                return;
            }

            this.State = TransmissionState.Disposed;
            this.gene0?.Dispose();
            this.gene1?.Dispose();
            this.gene2?.Dispose();
            if (this.genes is not null)
            {
                foreach (var x in this.genes)
                {
                    x.Dispose();
                }

                this.genes = default; // this.genes.Clear();
            }

            tcs = this.tcs;
            this.tcs = default;
        }

        tcs?.TrySetResult(NetResult.Closed);
    }

    internal NetResult SendBlock(uint primaryId, ulong secondaryId, ByteArrayPool.MemoryOwner block, TaskCompletionSource<NetResult>? tcs)
    {
        var size = sizeof(uint) + sizeof(ulong) + block.Span.Length;
        var info = CalculateGene((uint)size);

        lock (this.syncObject)
        {
            if (this.State != TransmissionState.Initial)
            {
                return NetResult.TransmissionConsumed;
            }

            this.State = TransmissionState.Sending;
            this.tcs = tcs;

            var span = block.Span;
            if (info.NumberOfGenes == 1)
            {// gene0
                this.gene0 = new(0);
                this.CreatePacket(0, info.NumberOfGenes, primaryId, secondaryId, span, out var owner);
                this.gene0.SetSend(owner);
            }
            else if (info.NumberOfGenes == 2)
            {// gene0, gene1
                this.gene0 = new(0);
                this.CreatePacket(0, info.NumberOfGenes, primaryId, secondaryId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                this.gene0.SetSend(owner);

                span = span.Slice((int)info.FirstGeneSize);
                this.gene1 = new(1);
                this.CreatePacket2(1, info.NumberOfGenes, span, out owner);
                this.gene1.SetSend(owner);
            }
            else if (info.NumberOfGenes == 3)
            {// gene0, gene1, gene2
                this.gene0 = new(0);
                this.CreatePacket(0, info.NumberOfGenes, primaryId, secondaryId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                this.gene0.SetSend(owner);

                span = span.Slice((int)info.FirstGeneSize);
                this.gene1 = new(1);
                this.CreatePacket2(1, info.NumberOfGenes, span.Slice(0, GeneFrame.MaxGeneLength), out owner);
                this.gene1.SetSend(owner);

                span = span.Slice(GeneFrame.MaxGeneLength);
                this.gene2 = new(2);
                this.CreatePacket2(2, info.NumberOfGenes, span, out owner);
                this.gene2.SetSend(owner);
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
            this.Connection.ConnectionTerminal.RegisterSend(this);
        }

        return NetResult.Success;
    }

    internal async Task<NetResponse> ReceiveBlock()
    {
        return new();
    }

    internal bool SendInternal(NetSender netSender)
    {
        lock (this.syncObject)
        {
            if (this.State != TransmissionState.Sending)
            {
                return false;
            }

            var endpoint = this.Connection.EndPoint.EndPoint;
            this.gene0?.Send(netSender, endpoint);
            this.gene1?.Send(netSender, endpoint);
            this.gene2?.Send(netSender, endpoint);

            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint NumberOfGenes, uint FirstGeneSize, uint LastGeneSize) CalculateGene(uint size)
    {// FirstGeneSize, GeneFrame.MaxBlockLength..., LastGeneSize
        if (size <= GeneFrame.MaxFirstGeneLength)
        {
            return (1, size, 0);
        }

        size -= GeneFrame.MaxFirstGeneLength;
        var numberOfGenes = size / GeneFrame.MaxGeneLength;
        var lastGeneSize = size - (numberOfGenes * GeneFrame.MaxGeneLength);
        return (GeneFrame.MaxFirstGeneLength, lastGeneSize > 0 ? numberOfGenes + 2 : numberOfGenes + 1, lastGeneSize);
    }

    private void CreatePacket(uint genePosition, uint geneTotal, uint primaryId, ulong secondaryId, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert(block.Length <= (GeneFrame.MaxGeneLength - sizeof(uint) - sizeof(ulong)));

        // GeneFrameCode
        Span<byte> frameHeader = stackalloc byte[GeneFrame.Length + sizeof(uint) + sizeof(ulong)];
        var span = frameHeader;

        BitConverter.TryWriteBytes(span, (ushort)FrameType.Block); // Frame type
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.TransmissionId); // TransmissionId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, genePosition); // GenePosition
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, geneTotal); // GeneMax
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, primaryId); // PrimaryId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, secondaryId); // SecondaryId
        span = span.Slice(sizeof(ulong));

        this.Connection.CreatePacket(frameHeader, block, out owner);
    }

    private void CreatePacket2(uint genePosition, uint geneTotal, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert(block.Length <= GeneFrame.MaxGeneLength);

        // GeneFrameCode
        Span<byte> frameHeader = stackalloc byte[GeneFrame.Length];
        var span = frameHeader;

        BitConverter.TryWriteBytes(span, (ushort)FrameType.Block); // Frame type
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.TransmissionId); // TransmissionId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, genePosition); // GenePosition
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, geneTotal); // GeneMax
        span = span.Slice(sizeof(uint));

        this.Connection.CreatePacket(frameHeader, block, out owner);
    }
}
