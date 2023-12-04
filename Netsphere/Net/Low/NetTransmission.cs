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
    private NetGene? sendGene; // Single gene
    private NetGene.GoshujinClass? sendGenes; // Multiple genes
    private NetGene? recvGene; // Single gene
    private NetGene.GoshujinClass? recvGenes; // Multiple genes

    #endregion

    public void Dispose()
    {
        lock (this.syncObject)
        {
            if (this.State == TransmissionState.Disposed)
            {
                return;
            }

            this.State = TransmissionState.Disposed;
            this.tcs?.TrySetResult(NetResult.Closed)
        }
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

    internal void SendInternal(NetSender netSender)
    {
        lock (this.syncObject)
        {
            if (this.State != TransmissionState.Sending)
            {
                return;
            }

            if (this.sendGene is not null)
            {
                netSender.Send_NotThreadSafe(this.Connection.EndPoint.EndPoint, transmission);
            }
        }
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
