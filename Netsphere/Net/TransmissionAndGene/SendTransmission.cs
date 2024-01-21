﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Collections;
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
internal sealed partial class SendTransmission : IDisposable
{
    private const int CongestionControlThreshold = 3;

    /* State transitions
     *  SendAndReceiveAsync (Client) : Initial -> Send/Receive Ack -> Receive -> Disposed
     *  SendAsync                   (Client) : Initial -> Send/Receive Ack -> tcs / Disposed
     *  (Server) : Initial -> Receive -> (Invoke) -> Disposed
     *  (Server) : Initial -> Receive -> (Invoke) -> Send/Receive Ack -> tcs / Disposed
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

    public string TransmissionIdText
        => ((ushort)this.TransmissionId).ToString("x4");

    public NetTransmissionMode Mode { get; private set; } // lock (this.syncObject)

    public int GeneSerialMax { get; private set; }

#pragma warning disable SA1401 // Fields should be private
    internal UnorderedLinkedList<SendTransmission>.Node? SendNode; // lock (ConnectionTerminal.SyncSend)
#pragma warning restore SA1401 // Fields should be private

    private readonly object syncObject = new();
    private TaskCompletionSource<NetResult>? sentTcs;
    private SendGene? gene0; // Gene 0
    private SendGene? gene1; // Gene 1
    private SendGene? gene2; // Gene 2
    private SendGene.GoshujinClass? genes; // Multiple genes
    private int sendGeneSerial;

    #endregion

    public void Dispose()
    {
        this.Connection.RemoveTransmission(this);
        this.DisposeTransmission();
    }

    internal void DisposeTransmission()
    {
        lock (this.syncObject)
        {
            this.DisposeInternal();
        }
    }

    internal void DisposeInternal()
    {// lock (this.syncObject)
        // Console.WriteLine($"Dispose send transmission: {this.Connection.ToString()} {this.TransmissionIdText} {this.Mode.ToString()}");
        if (this.Mode == NetTransmissionMode.Disposed)
        {
            return;
        }

        this.Mode = NetTransmissionMode.Disposed;
        this.gene0?.Dispose(false);
        this.gene1?.Dispose(false);
        this.gene2?.Dispose(false);
        if (this.genes is not null)
        {
            foreach (var x in this.genes)
            {
                x.DisposeMemory();
            }

            this.genes = default; // this.genes.Clear();
        }

        if (this.sentTcs is not null)
        {
            this.sentTcs.SetResult(NetResult.Closed);
            this.sentTcs = null;
        }
    }

    internal void SetResend(SendGene sendGene)
    {
        lock (this.syncObject)
        {
            Debug.Assert(sendGene.SendTransmission == this);

            sendGene.SetResend();
            if (this.Mode == NetTransmissionMode.Block && this.genes is not null)
            {
                if (sendGene.GeneSerial < this.GeneSerialMax &&
                    this.sendGeneSerial > sendGene.GeneSerial)
                {
                    this.sendGeneSerial = sendGene.GeneSerial;
                }
            }
        }
    }

    internal ProcessSendResult ProcessSingleSend(NetSender netSender)
    {// lock (this.ConnectionTerminal.SyncSend)
        lock (this.syncObject)
        {
            if (this.Mode == NetTransmissionMode.Rama)
            {
                if (this.gene0?.IsSent == false)
                {
                    if (!this.gene0.Send_NotThreadSafe(netSender, 0))
                    {// Cannot send
                        return ProcessSendResult.Complete;
                    }
                }

                if (this.gene1?.IsSent == false)
                {
                    if (!this.gene1.Send_NotThreadSafe(netSender, 1))
                    {// Cannot send
                        return ProcessSendResult.Complete;
                    }
                }

                if (this.gene2?.IsSent == false)
                {
                    if (!this.gene2.Send_NotThreadSafe(netSender, 2))
                    {// Cannot send
                        return ProcessSendResult.Complete;
                    }
                }

                return ProcessSendResult.Complete;
            }
            else if (this.Mode == NetTransmissionMode.Block && this.genes is not null)
            {
                while (this.sendGeneSerial < this.GeneSerialMax)
                {
                    if (this.genes.GeneSerialListChain.Get(this.sendGeneSerial++) is { } gene)
                    {
                        if (!gene.Send_NotThreadSafe(netSender, 0))
                        {// Cannot send
                            return ProcessSendResult.Complete;
                        }

                        return this.sendGeneSerial >= this.GeneSerialMax ? ProcessSendResult.Complete : ProcessSendResult.Remaining;
                    }
                }

                return ProcessSendResult.Complete;
            }
            else if (this.Mode == NetTransmissionMode.Stream)
            {
                return ProcessSendResult.Complete;
            }
            else
            {
                return ProcessSendResult.Complete;
            }
        }
    }

    internal NetResult SendBlock(uint dataKind, ulong dataId, ByteArrayPool.MemoryOwner block, TaskCompletionSource<NetResult>? sentTcs)
    {
        var info = NetHelper.CalculateGene(block.Span.Length);
        // Console.WriteLine($"SendBlock: {info.NumberOfGenes} genes");

        lock (this.syncObject)
        {
            if (this.Connection.IsClosedOrDisposed ||
                this.Mode != NetTransmissionMode.Initial)
            {
                return NetResult.Closed;
            }

            this.Connection.UpdateLatestAckMics();
            this.sentTcs = sentTcs;

            if (this.Connection.SendTransmissionsCount >= CongestionControlThreshold)
            {// Enable congestion control when the number of SendTransmissions exceeds the threshold.
                this.Connection.CreateCongestionControl();
            }

            var span = block.Span;
            if (info.NumberOfGenes <= NetHelper.RamaGenes)
            {// Rama
                this.Mode = NetTransmissionMode.Rama;
                if (info.NumberOfGenes == 1)
                {// gene0
                    this.GeneSerialMax = info.NumberOfGenes;
                    this.gene0 = new(this);

                    this.CreateFirstPacket(0, info.NumberOfGenes, dataKind, dataId, span, out var owner);
                    this.gene0.SetSend(owner);
                }
                else if (info.NumberOfGenes == 2)
                {// gene0, gene1
                    this.GeneSerialMax = info.NumberOfGenes;
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
                    this.GeneSerialMax = info.NumberOfGenes;
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
                this.Connection.CreateCongestionControl();

                this.GeneSerialMax = info.NumberOfGenes;
                this.genes = new();
                this.genes.GeneSerialListChain.Resize(info.NumberOfGenes);

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

        this.Connection.AddSend(this);

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
        }

        return NetResult.Success;
    }

    internal bool ProcessReceive_Ack(scoped Span<byte> span/*, ref int acked*/)
    {// lock (SendTransmissions.syncObject)
        var completeFlag = false;

        long sentMics = 0;
        lock (this.syncObject)
        {
            while (span.Length >= 8)
            {
                var startGene = BitConverter.ToInt32(span);
                span = span.Slice(sizeof(int));
                var endGene = BitConverter.ToInt32(span);
                span = span.Slice(sizeof(int));

                if (startGene < 0 || startGene >= this.GeneSerialMax ||
                    endGene < 0 || endGene > this.GeneSerialMax)
                {
                    continue;
                }

                this.Connection.UpdateLatestAckMics();

                if (this.Mode == NetTransmissionMode.Rama)
                {
                    if (startGene == 0 && endGene == this.GeneSerialMax)
                    {
                        this.Connection.Logger.TryGet(LogLevel.Debug)?.Log($"{this.Connection.ConnectionIdText} ReceiveAck Rama 0 - {this.GeneSerialMax}");

                        if (this.gene0 is not null)
                        {
                            sentMics = Math.Max(sentMics, this.gene0.SentMics);
                            this.gene0.Dispose(true);
                            this.gene0 = null;
                        }

                        if (this.gene1 is not null)
                        {
                            sentMics = Math.Max(sentMics, this.gene1.SentMics);
                            this.gene1.Dispose(true);
                            this.gene1 = null;
                        }

                        if (this.gene2 is not null)
                        {
                            sentMics = Math.Max(sentMics, this.gene2.SentMics);
                            this.gene2.Dispose(true);
                            this.gene2 = null;
                        }

                        completeFlag = true;
                        // acked += this.totalGene;
                        break;
                    }
                }
                else if (this.Mode == NetTransmissionMode.Block && this.genes is not null)
                {
                    this.Connection.Logger.TryGet(LogLevel.Debug)?.Log($"{this.Connection.ConnectionIdText} ReceiveAck {startGene} - {endGene - 1}");
                    for (var i = startGene; i < endGene; i++)
                    {
                        if (this.genes.GeneSerialListChain.Get(i) is { } gene)
                        {
                            // this.Connection.Logger.TryGet(LogLevel.Debug)?.Log($"{this.Connection.ConnectionIdText} Dispose send gene {gene.GeneSerial}");
                            sentMics = Math.Max(sentMics, gene.SentMics);
                            gene.Dispose(true); // this.genes.GeneSerialListChain.Remove(gene);
                            // acked++;
                        }
                    }

                    completeFlag = this.genes.GeneSerialListChain.Count == 0;
                }
                else
                {
                    return false;
                }
            }

            var rtt = Mics.FastSystem - sentMics;
            if (rtt > 0)
            {
                this.Connection.AddRtt((int)rtt);
            }

            if (completeFlag)
            {// Send transmission complete
                if (this.sentTcs is not null)
                {
                    this.sentTcs.SetResult(NetResult.Success);
                    this.sentTcs = null;
                }

                // Remove from sendTransmissions and dispose.
                this.Goshujin = null;
                this.DisposeInternal();
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

    private void CreateFollowingPacket(/*int geneSerial, */int dataPosition, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert(block.Length <= FollowingGeneFrame.MaxGeneLength);

        // FollowingGeneFrameCode
        Span<byte> frameHeader = stackalloc byte[FollowingGeneFrame.Length];
        var span = frameHeader;

        BitConverter.TryWriteBytes(span, (ushort)FrameType.FollowingGene); // Frame type
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.TransmissionId); // TransmissionId
        span = span.Slice(sizeof(uint));

        // BitConverter.TryWriteBytes(span, geneSerial); // GeneSerial
        // span = span.Slice(sizeof(int));

        BitConverter.TryWriteBytes(span, dataPosition); // DataPosition
        span = span.Slice(sizeof(int));

        Debug.Assert(span.Length == 0);
        this.Connection.CreatePacket(frameHeader, block, out owner);
    }
}
