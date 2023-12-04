// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Netsphere.Packet;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class NetTransmission : IDisposable
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
        Complete,
        Disposed,
    }

    [Link(Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false)]
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

                // this.genes.Clear(); // tempcode
            }

            tcs = this.tcs;
            this.tcs = default;
        }

        tcs?.TrySetResult(NetResult.Closed);
    }

    internal NetResult SendBlock(uint primaryId, ulong secondaryId, ByteArrayPool.MemoryOwner block, TaskCompletionSource<NetResult>? tcs)
    {
        var size = sizeof(uint) + sizeof(ulong) + block.Span.Length;
        var info = CalculateGene(size);

        lock (this.syncObject)
        {
            if (this.State != TransmissionState.Initial)
            {
                return NetResult.TransmissionConsumed;
            }

            this.State = TransmissionState.Sending;
            this.tcs = tcs;

            if (info.NumberOfGenes == 1)
            {// gene0
                this.gene0 = new(0);
                this.CreatePacket(0, info.NumberOfGenes, primaryId, secondaryId, block.Span, out var owner);
                this.gene0.SetSend(owner);
            }
            else if (info.NumberOfGenes == 2)
            {// gene0, gene1
                this.gene0 = new(0);
                this.CreatePacket(0, info.NumberOfGenes, primaryId, secondaryId, block.Span, out var owner);
                this.gene0.SetSend(owner);

                this.gene1 = new(1);
                this.CreatePacket2(1, info.NumberOfGenes, block.Span, out owner);
                this.gene1.SetSend(owner);
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

    internal void SendInternal(NetSender netSender)
    {
        lock (this.syncObject)
        {
            if (this.State != TransmissionState.Sending)
            {
                return;
            }

            if (this.gene0 is not null)
            {
                netSender.Send_NotThreadSafe(this.Connection.EndPoint.EndPoint, transmission);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint NumberOfGenes, uint LastGeneSize) CalculateGene(int size)
    {
        var numberOfGenes = (uint)(size / GeneFrame.MaxBlockLength);
        var lastGeneSize = (uint)(size - (numberOfGenes * GeneFrame.MaxBlockLength));
        return (lastGeneSize > 0 ? numberOfGenes + 1 : numberOfGenes, lastGeneSize);
    }

    private bool CreatePacket(uint genePosition, uint geneTotal, uint primaryId, ulong secondaryId, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert(block.Length <= GeneFrame.MaxBlockLength);

        // GeneFrameeCode
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

        return this.Connection.CreatePacket(frameHeader, block, out owner);
    }

    private bool CreatePacket2(uint genePosition, uint geneTotal, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert(block.Length <= GeneFrame.MaxBlockLength);

        // GeneFrameeCode
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

        return this.Connection.CreatePacket(frameHeader, block, out owner);
    }
}
