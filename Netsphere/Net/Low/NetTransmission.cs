// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Netsphere.Packet;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class NetTransmission // : IDisposable
{
    internal const int BlockThreshold = 3;

    /* State transitions
     *  SendAndReceiveAsync (Client) : Sending -> Receiving -> Disposed
     *  SendAsync                   (Client) : Sending -> tcs / Disposed
     *  (Server) : Receiving -> Received -> Disposed
     *  (Server) : Receiving -> Received -> Sending -> tcs / Disposed
     */
    public enum TransmissionState
    {
        Sending,
        Receiving,
        Received,
        Disposed,
    }

    public enum TransmissionMode
    {
        Rama,
        Block,
        Stream,
    }

    [Link(Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false)]
    [Link(Name = "ResendQueue", Type = ChainType.QueueList, AutoLink = false)]
    public NetTransmission(Connection connection, bool isClient, uint transmissionId)
    {
        this.Connection = connection;
        this.TransmissionId = transmissionId;

        this.IsClient = isClient;
        this.State = TransmissionState.Sending;
    }

    #region FieldAndProperty

    public Connection Connection { get; }

    public bool IsClient { get; }

    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint TransmissionId { get; }

    public TransmissionState State { get; private set; } // lock (this.syncObject)

    public TransmissionMode Mode
    {
        get
        {
            if (this.genes is { } genes)
            {
                if (genes.Count <= this.Connection.Agreement.MaxBlockGenes)
                {
                    return TransmissionMode.Block;
                }
                else
                {
                    return TransmissionMode.Stream;
                }
            }
            else
            {
                return TransmissionMode.Rama;
            }
        }
    }

    private readonly object syncObject = new();
    private uint totalGene;
    private TaskCompletionSource<NetResult>? tcs;
    private NetGene? gene0; // Gene 0
    private NetGene? gene1; // Gene 1
    private NetGene? gene2; // Gene 2
    private NetGene.GoshujinClass? genes; // Multiple genes

    #endregion

    public void SetReceive(uint totalGene)
    {
        this.State = TransmissionState.Receiving;
        this.totalGene = totalGene;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (uint NumberOfGenes, uint FirstGeneSize, uint LastGeneSize) CalculateGene(long size)
    {// FirstGeneSize, GeneFrame.MaxBlockLength..., LastGeneSize
        if (size <= FirstGeneFrame.MaxGeneLength)
        {
            return (1, (uint)size, 0);
        }

        size -= FirstGeneFrame.MaxGeneLength;
        var numberOfGenes = (uint)(size / FollowingGeneFrame.MaxGeneLength);
        var lastGeneSize = (uint)(size - (numberOfGenes * FollowingGeneFrame.MaxGeneLength));
        return (FirstGeneFrame.MaxGeneLength, lastGeneSize > 0 ? numberOfGenes + 2 : numberOfGenes + 1, lastGeneSize);
    }

    internal void DisposeInternal()
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
        var info = CalculateGene(block.Span.Length);

        lock (this.syncObject)
        {
            Debug.Assert(this.State == TransmissionState.Sending);

            this.State = TransmissionState.Sending;
            this.tcs = tcs;
            this.totalGene = info.NumberOfGenes;

            var span = block.Span;
            if (info.NumberOfGenes == 1)
            {// gene0
                this.gene0 = new();
                this.CreateFirstPacket(info.NumberOfGenes, primaryId, secondaryId, span, out var owner);
                this.gene0.SetSend(owner);
            }
            else if (info.NumberOfGenes == 2)
            {// gene0, gene1
                this.gene0 = new();
                this.CreateFirstPacket(info.NumberOfGenes, primaryId, secondaryId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                this.gene0.SetSend(owner);

                span = span.Slice((int)info.FirstGeneSize);
                Debug.Assert(span.Length == info.LastGeneSize);
                this.gene1 = new();
                this.CreateFollowingPacket(1, span, out owner);
                this.gene1.SetSend(owner);
            }
            else if (info.NumberOfGenes == 3)
            {// gene0, gene1, gene2
                this.gene0 = new();
                this.CreateFirstPacket(info.NumberOfGenes, primaryId, secondaryId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                this.gene0.SetSend(owner);

                span = span.Slice((int)info.FirstGeneSize);
                this.gene1 = new();
                this.CreateFollowingPacket(1, span.Slice(0, FollowingGeneFrame.MaxGeneLength), out owner);
                this.gene1.SetSend(owner);

                span = span.Slice(FollowingGeneFrame.MaxGeneLength);
                Debug.Assert(span.Length == info.LastGeneSize);
                this.gene2 = new();
                this.CreateFollowingPacket(2, span, out owner);
                this.gene2.SetSend(owner);
            }
            else
            {// Multiple genes
                if (info.NumberOfGenes > this.Connection.Agreement.MaxBlockGenes)
                {
                    return NetResult.BlockSizeLimit;
                }

                this.genes = new();
                this.genes.SlidingListChain.Resize((int)info.NumberOfGenes);

                var firstGene = new NetGene();
                this.CreateFirstPacket(info.NumberOfGenes, primaryId, secondaryId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                firstGene.SetSend(owner);
                span = span.Slice((int)info.FirstGeneSize);
                firstGene.Goshujin = this.genes;
                this.genes.SlidingListChain.Add(firstGene);

                for (uint i = 1; i < info.NumberOfGenes; i++)
                {
                    var size = (int)(i == info.NumberOfGenes - 1 ? info.LastGeneSize : FollowingGeneFrame.MaxGeneLength);
                    var gene = new NetGene();
                    this.CreateFollowingPacket(i, span.Slice(0, size), out owner);
                    gene.SetSend(owner);

                    span = span.Slice(size);
                    gene.Goshujin = this.genes;
                    this.genes.SlidingListChain.Add(gene);
                }

                Debug.Assert(span.Length == 0);
            }
        }

        if (info.NumberOfGenes > BlockThreshold)
        {// Flow control
        }
        else
        {
            this.Connection.ConnectionTerminal.RegisterSend(this);
        }

        return NetResult.Success;
    }

    internal void ProcessReceive_Gene(uint genePosition, ByteArrayPool.MemoryOwner toBeShared)
    {
        var completeFlag = false;
        var serverFlag = false;
        ByteArrayPool.MemoryOwner owner;
        lock (this.syncObject)
        {
            if (this.State == TransmissionState.Receiving &&
                genePosition < this.totalGene)
            {// Set gene
                if (this.totalGene <= BlockThreshold)
                {// Single send/recv
                    if (genePosition == 0)
                    {
                        this.gene0 ??= new();
                        this.gene0.SetRecv(toBeShared);
                    }
                    else if (genePosition == 1)
                    {
                        this.gene1 ??= new();
                        this.gene1.SetRecv(toBeShared);
                    }
                    else if (genePosition == 2)
                    {
                        this.gene2 ??= new();
                        this.gene2.SetRecv(toBeShared);
                    }

                    if (this.totalGene == 0)
                    {
                        completeFlag = true;
                    }
                    else if (this.totalGene == 1)
                    {
                        completeFlag =
                            this.gene0?.IsReceived == true;
                    }
                    else if (this.totalGene == 2)
                    {
                        completeFlag =
                            this.gene0?.IsReceived == true &&
                            this.gene1?.IsReceived == true;
                    }
                    else if (this.totalGene == 3)
                    {
                        completeFlag =
                            this.gene0?.IsReceived == true &&
                            this.gene1?.IsReceived == true &&
                            this.gene2?.IsReceived == true;
                    }
                }
                else
                {// Multiple send/recv
                }

                if (completeFlag)
                {// Complete
                    serverFlag = this.ProcessReceive_GeneComplete(out owner);
                }
            }
        }

        if (this.totalGene <= BlockThreshold)
        {// Fast ack
            if (completeFlag)
            {
                Span<byte> ackFrame = stackalloc byte[2 + (8 * 3)];
                var span = ackFrame;
                BitConverter.TryWriteBytes(span, (ushort)FrameType.Ack);
                span = span.Slice(sizeof(ushort));

                if (this.totalGene == 1)
                {
                    BitConverter.TryWriteBytes(span, this.TransmissionId);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, 0u);
                    span = span.Slice(sizeof(uint));
                }
                else if (this.totalGene == 2)
                {
                    BitConverter.TryWriteBytes(span, this.TransmissionId);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, 0u);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, this.TransmissionId);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, 1u);
                    span = span.Slice(sizeof(uint));
                }
                else if (this.totalGene == 3)
                {
                    BitConverter.TryWriteBytes(span, this.TransmissionId);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, 0u);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, this.TransmissionId);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, 1u);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, this.TransmissionId);
                    span = span.Slice(sizeof(uint));
                    BitConverter.TryWriteBytes(span, 2u);
                    span = span.Slice(sizeof(uint));
                }

                this.Connection.SendPriorityFrame(ackFrame.Slice(0, 2 + (8 * (int)this.totalGene)));
            }
        }
        else
        {// Ack (TransmissionId, GenePosition)
            // this.Connection.AddAck(this.TransmissionId, genePosition);
        }

        if (serverFlag)
        {// Connection, NetTransmission, Owner
        }
    }

    internal bool ProcessReceive_GeneComplete(out ByteArrayPool.MemoryOwner toBeMoved)
    {
        int length;
        toBeMoved = default;

        if (this.genes is null)
        {// Single send/recv
            if (this.totalGene == 0)
            {
                length = 0;
            }
            else
            {
                var firstPacket = this.gene0!.Packet.Slice(12);
                length = firstPacket.Span.Length;
                if (this.totalGene == 1)
                {
                    toBeMoved = firstPacket.IncrementAndShare();
                }
                else if (this.totalGene == 2)
                {
                    length += this.gene1!.Packet.Span.Length;
                    toBeMoved = ByteArrayPool.Default.Rent(length).ToMemoryOwner(0, length);

                    var span = toBeMoved.Span;
                    firstPacket.Span.CopyTo(span);
                    span = span.Slice(firstPacket.Span.Length);
                    this.gene1!.Packet.Span.CopyTo(span);
                }
                else if (this.totalGene == 3)
                {
                    length += this.gene1!.Packet.Span.Length;
                    length += this.gene2!.Packet.Span.Length;
                    toBeMoved = ByteArrayPool.Default.Rent(length).ToMemoryOwner(0, length);

                    var span = toBeMoved.Span;
                    firstPacket.Span.CopyTo(span);
                    span = span.Slice(firstPacket.Span.Length);
                    this.gene1!.Packet.Span.CopyTo(span);
                    span = span.Slice(this.gene1!.Packet.Span.Length);
                    this.gene2!.Packet.Span.CopyTo(span);
                }
            }
        }
        else
        {// Multiple send/recv
        }

        if (this.IsClient)
        {// Client
            //this.DisposeInternal();
        }
        else
        {// Server
            return true;
        }

        if (this.tcs is not null)
        {
            // this.tcs.SetResult
        }

        return false;
    }

    internal bool ProcessReceive_Ack(uint genePosition)
    {
        var completeFlag = false;
        lock (this.syncObject)
        {
            if (this.State == TransmissionState.Sending &&
                genePosition < this.totalGene)
            {
                if (this.genes is null)
                {// Single send/recv
                    if (genePosition == 0)
                    {
                        this.gene0?.SetAck();
                    }
                    else if (genePosition == 1)
                    {
                        this.gene1?.SetAck();
                    }
                    else if (genePosition == 2)
                    {
                        this.gene2?.SetAck();
                    }
                }
                else
                {// Multiple send/recv
                }

                if (this.totalGene == 0)
                {
                    completeFlag = true;
                }
                else if (this.totalGene == 1)
                {
                    completeFlag =
                        this.gene0?.IsComplete == true;
                }
                else if (this.totalGene == 2)
                {
                    completeFlag =
                        this.gene0?.IsComplete == true &&
                        this.gene1?.IsComplete == true;
                }
                else if (this.totalGene == 3)
                {
                    completeFlag =
                        this.gene0?.IsComplete == true &&
                        this.gene1?.IsComplete == true &&
                        this.gene2?.IsComplete == true;
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
        }

        return completeFlag;
    }

    internal long GetLargestSentMics()
    {
        long mics = 0;
        lock (this.syncObject)
        {
            if (this.gene0 is not null)
            {
                mics = mics > this.gene0.SentMics ? mics : this.gene0.SentMics;
            }

            if (this.gene1 is not null)
            {
                mics = mics > this.gene1.SentMics ? mics : this.gene1.SentMics;
            }

            if (this.gene2 is not null)
            {
                mics = mics > this.gene2.SentMics ? mics : this.gene2.SentMics;
            }
        }

        return mics;
    }

    /*internal bool CheckResend(NetSender netSender)
    {
        lock (this.syncObject)
        {
            if (this.State != TransmissionState.Sending)
            {
                return false;
            }

            if (this.gene0 is not null && this.gene0.CheckResend(netSender))
            {
                return true;
            }
            else if (this.gene1 is not null && this.gene1.CheckResend(netSender))
            {
                return true;
            }
            else if (this.gene2 is not null && this.gene2.CheckResend(netSender))
            {
                return true;
            }
        }

        return false;
    }*/

    internal bool SendInternal(NetSender netSender, out int sentCount)
    {
        sentCount = 0;
        if (this.Connection.IsClosedOrDisposed)
        {
            //this.DisposeInternal();
            return false;
        }

        lock (this.syncObject)
        {
            if (this.State != TransmissionState.Sending)
            {
                return false;
            }

            var endpoint = this.Connection.EndPoint.EndPoint;
            if (this.genes is null)
            {// Single send
                this.gene0?.Send(netSender, endpoint, ref sentCount);
                this.gene1?.Send(netSender, endpoint, ref sentCount);
                this.gene2?.Send(netSender, endpoint, ref sentCount);
            }
            else
            {// Multiple send
            }

            return true;
        }
    }

    private void CreateFirstPacket(uint totalGene, uint primaryId, ulong secondaryId, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert(block.Length <= FirstGeneFrame.MaxGeneLength);

        // FirstGeneFrameCode
        Span<byte> frameHeader = stackalloc byte[FirstGeneFrame.Length];
        var span = frameHeader;

        BitConverter.TryWriteBytes(span, (ushort)FrameType.FirstGene); // Frame type
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.TransmissionId); // TransmissionId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, totalGene); // TotalGene
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, primaryId); // PrimaryId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, secondaryId); // SecondaryId
        span = span.Slice(sizeof(ulong));

        Debug.Assert(span.Length == 0);
        this.Connection.CreatePacket(frameHeader, block, out owner);
    }

    private void CreateFollowingPacket(uint genePosition, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
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
        span = span.Slice(sizeof(uint));

        Debug.Assert(span.Length == 0);
        this.Connection.CreatePacket(frameHeader, block, out owner);
    }
}
