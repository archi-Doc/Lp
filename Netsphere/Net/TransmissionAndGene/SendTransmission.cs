// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere.Packet;

namespace Netsphere.Net;

public enum NetTransmissionMode
{
    Initial,
    Rama,
    Block,
    Stream,
    Disposed,
}

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class SendTransmission : IDisposable
{
    /* State transitions
     *  SendAndReceiveAsync (Client) : Initial -> Sending -> Receiving -> Disposed
     *  SendAsync                   (Client) : Initial -> Sending -> tcs / Disposed
     *  (Server) : Initial -> Receiving -> (Invoke) -> Disposed
     *  (Server) : Initial -> Receiving -> (Invoke) -> Sending -> tcs / Disposed
     */

    public SendTransmission(Connection connection, uint transmissionId)
    {
        this.Connection = connection;
        this.TransmissionId = transmissionId;
    }

    #region FieldAndProperty

    public Connection Connection { get; }

    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint TransmissionId { get; }

    public NetTransmissionMode Mode { get; private set; } // lock (this.syncObject)

    private readonly object syncObject = new();
    private int totalGene;
    private TaskCompletionSource<NetResponse>? tcs;
    private SendGene? gene0; // Gene 0
    private SendGene? gene1; // Gene 1
    private SendGene? gene2; // Gene 2
    private SendGene.GoshujinClass? genes; // Multiple genes

    #endregion

    public void Dispose()
    {
        this.Connection.RemoveTransmission(this);
        this.DisposeInternal();
    }

    internal void DisposeInternal()
    {
        TaskCompletionSource<NetResponse>? tcs = default;

        lock (this.syncObject)
        {
            if (this.Mode == NetTransmissionMode.Disposed)
            {
                return;
            }

            this.Mode = NetTransmissionMode.Disposed;
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

        tcs?.TrySetResult(new(NetResult.Closed));
    }

    internal NetResult SendBlock(uint dataKind, ulong dataId, ByteArrayPool.MemoryOwner block, TaskCompletionSource<NetResponse> tcs, bool requiresResponse)
    {
        var info = NetHelper.CalculateGene(block.Span.Length);

        lock (this.syncObject)
        {
            Debug.Assert(this.Mode == NetTransmissionMode.Initial);

            this.tcs = tcs;
            this.totalGene = info.NumberOfGenes;

            var span = block.Span;
            if (info.NumberOfGenes <= NetHelper.RamaGenes)
            {// Rama
                this.Mode = NetTransmissionMode.Rama;
                if (info.NumberOfGenes == 1)
                {// gene0
                    this.gene0 = new(this);

                    this.CreateFirstPacket(0, info.NumberOfGenes, dataKind, dataId, span, out var owner);
                    this.gene0.SetSend(owner);
                }
                else if (info.NumberOfGenes == 2)
                {// gene0, gene1
                    this.gene0 = new(this);
                    this.gene1 = new(this);

                    this.CreateFirstPacket(0, info.NumberOfGenes, dataKind, dataId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                    this.gene0.SetSend(owner);

                    span = span.Slice((int)info.FirstGeneSize);
                    Debug.Assert(span.Length == info.LastGeneSize);
                    this.CreateFollowingPacket(1, span, out owner);
                    this.gene1.SetSend(owner);
                }
                else if (info.NumberOfGenes == 3)
                {// gene0, gene1, gene2
                    this.gene0 = new(this);
                    this.gene1 = new(this);
                    this.gene2 = new(this);

                    this.CreateFirstPacket(0, info.NumberOfGenes, dataKind, dataId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                    this.gene0.SetSend(owner);

                    span = span.Slice((int)info.FirstGeneSize);
                    this.CreateFollowingPacket(1, span.Slice(0, FollowingGeneFrame.MaxGeneLength), out owner);
                    this.gene1.SetSend(owner);

                    span = span.Slice(FollowingGeneFrame.MaxGeneLength);
                    Debug.Assert(span.Length == info.LastGeneSize);
                    this.CreateFollowingPacket(2, span, out owner);
                    this.gene2.SetSend(owner);
                }
                else
                {
                    return NetResult.UnknownException;
                }
            }
            else
            {// Multiple genes
                if (info.NumberOfGenes > this.Connection.Agreement.MaxBlockGenes)
                {
                    return NetResult.BlockSizeLimit;
                }

                this.Mode = NetTransmissionMode.Block;
                this.Connection.CreateFlowControl();

                this.genes = new();
                this.genes.GeneSerialListChain.Resize((int)info.NumberOfGenes);

                var firstGene = new SendGene(this);
                this.CreateFirstPacket(0, info.NumberOfGenes, dataKind, dataId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                firstGene.SetSend(owner);
                span = span.Slice((int)info.FirstGeneSize);
                firstGene.Goshujin = this.genes;
                this.genes.GeneSerialListChain.Add(firstGene);

                for (var i = 1; i < info.NumberOfGenes; i++)
                {
                    var size = (int)(i == info.NumberOfGenes - 1 ? info.LastGeneSize : FollowingGeneFrame.MaxGeneLength);
                    var gene = new SendGene(this);
                    this.CreateFollowingPacket(i, span.Slice(0, size), out owner);
                    gene.SetSend(owner);

                    span = span.Slice(size);
                    gene.Goshujin = this.genes;
                    this.genes.GeneSerialListChain.Add(gene);
                }

                Debug.Assert(span.Length == 0);
            }
        }

        return NetResult.Success;
    }

    internal NetResult SendStream(uint dataKind, ulong dataId, long size, bool requiresResponse)
    {
        var info = NetHelper.CalculateGene(size);

        lock (this.syncObject)
        {
            Debug.Assert(this.Mode == NetTransmissionMode.Initial);

            if (info.NumberOfGenes > this.Connection.Agreement.MaxStreamGenes)
            {
                return NetResult.StreamSizeLimit;
            }

            this.Mode = NetTransmissionMode.Stream;
            this.totalGene = info.NumberOfGenes;
        }

        return NetResult.Success;
    }

    internal bool ProcessReceive_Ack(int genePosition)
    {
        var completeFlag = false;
        lock (this.syncObject)
        {
            if (this.Mode == NetTransmissionMode.Rama)
            {// Single send/recv
                if (genePosition == 0 && this.gene0 is not null)
                {
                    this.gene0.Dispose();
                    this.gene0 = null;
                }
                else if (genePosition == 1 && this.gene1 is not null)
                {
                    this.gene1.Dispose();
                    this.gene1 = null;
                }
                else if (genePosition == 2 && this.gene2 is not null)
                {
                    this.gene2.Dispose();
                    this.gene2 = null;
                }

                if (this.totalGene == 0)
                {
                    completeFlag = true;
                }
                else
                {
                    completeFlag =
                        this.gene0 == null &&
                        this.gene1 == null &&
                        this.gene2 == null;
                }
            }
            else if (this.Mode == NetTransmissionMode.Block && this.genes is not null)
            {// Multiple send/recv
                if (this.genes.GeneSerialListChain.Get(genePosition) is { } gene)
                {
                    this.genes.GeneSerialListChain.Remove(gene);
                }
                else
                {
                    return false;
                }
            }

            if (completeFlag)
            {
                if (this.tcs is null)
                {// Receive
                }
                else
                {// Tcs
                }
            }
        }

        return completeFlag;
    }

    internal bool ProcessReceive_Ack(scoped Span<byte> span)
    {
        var completeFlag = false;

        lock (this.syncObject)
        {
            while (span.Length >= 8)
            {
                var startGene = BitConverter.ToInt32(span);
                span = span.Slice(sizeof(int));
                var endGene = BitConverter.ToInt32(span);
                span = span.Slice(sizeof(int));

                if (startGene < 0 || startGene >= this.totalGene ||
                    endGene < 0 || endGene > this.totalGene)
                {
                    continue;
                }

                if (this.Mode == NetTransmissionMode.Rama)
                {
                    if (endGene == this.totalGene)
                    {
                        this.gene0?.Dispose();
                        this.gene0 = null;
                        this.gene1?.Dispose();
                        this.gene1 = null;
                        this.gene2?.Dispose();
                        this.gene2 = null;

                        completeFlag = true;
                        break;
                    }
                }
                else if (this.Mode == NetTransmissionMode.Block && this.genes is not null)
                {
                    for (var i = startGene; i < endGene; i++)
                    {
                        if (this.genes.GeneSerialListChain.Get(i) is { } gene)
                        {
                            this.genes.GeneSerialListChain.Remove(gene);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        if (completeFlag)
        {
            if (this.tcs is null)
            {// Receive
            }
            else
            {// Tcs
            }
        }

        return completeFlag;
    }

    private void CreateFirstPacket(ushort transmissionMode, int totalGene, uint dataKind, ulong dataId, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert(block.Length <= FirstGeneFrame.MaxGeneLength);

        // FirstGeneFrameCode
        Span<byte> frameHeader = stackalloc byte[FirstGeneFrame.Length];
        var span = frameHeader;

        BitConverter.TryWriteBytes(span, (ushort)FrameType.FirstGene); // Frame type
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, transmissionMode); // TransmissionMode
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.TransmissionId); // TransmissionId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, this.Connection.SmoothedRtt); // Rtt hint
        span = span.Slice(sizeof(int));

        BitConverter.TryWriteBytes(span, totalGene); // TotalGene
        span = span.Slice(sizeof(int));

        BitConverter.TryWriteBytes(span, dataKind); // Data kind
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, dataId); // Data id
        span = span.Slice(sizeof(ulong));

        Debug.Assert(span.Length == 0);
        this.Connection.CreatePacket(frameHeader, block, out owner);
    }

    private void CreateFollowingPacket(int genePosition, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert(block.Length <= FollowingGeneFrame.MaxGeneLength);

        // FollowingGeneFrameCode
        Span<byte> frameHeader = stackalloc byte[FollowingGeneFrame.Length];
        var span = frameHeader;

        BitConverter.TryWriteBytes(span, (ushort)FrameType.FollowingGene); // Frame type
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.TransmissionId); // TransmissionId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, genePosition); // GenePosition
        span = span.Slice(sizeof(int));

        Debug.Assert(span.Length == 0);
        this.Connection.CreatePacket(frameHeader, block, out owner);
    }
}
