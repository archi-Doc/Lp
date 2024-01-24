// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using Arc.Collections;
using Netsphere.Packet;
using Netsphere.Server;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
internal sealed partial class ReceiveTransmission : IDisposable
{
    // [Link(Name = "DisposedList", Type = ChainType.QueueList, AutoLink = false)]
    public ReceiveTransmission(Connection connection, uint transmissionId, TaskCompletionSource<NetResponse>? receivedTcs, ReceiveStream? receiveStream)
    {
        this.Connection = connection;
        this.TransmissionId = transmissionId;
        this.receivedTcs = receivedTcs;
        this.receiveStream = receiveStream;
    }

    #region FieldAndProperty

    public Connection Connection { get; }

    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint TransmissionId { get; }

    public NetTransmissionMode Mode { get; private set; } // lock (this.syncObject)

#pragma warning disable SA1401 // Fields should be private
    // Received/Disposed list, lock (Connection.receiveTransmissions.SyncObject)
    internal UnorderedLinkedList<ReceiveTransmission>.Node? ReceivedDisposedNode;
    internal long ReceivedDisposedMics;
    internal Queue<int>? AckGene; // lock(AckBuffer.syncObject)
#pragma warning restore SA1401 // Fields should be private

    private readonly object syncObject = new();
    private int totalGene;
    private TaskCompletionSource<NetResponse>? receivedTcs;
    private ReceiveStream? receiveStream;
    private int maxReceivedPosition;
    private ReceiveGene? gene0; // Gene 0
    private ReceiveGene? gene1; // Gene 1
    private ReceiveGene? gene2; // Gene 2
    private ReceiveGene.GoshujinClass? genes; // Multiple genes

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

        if (this.receivedTcs is not null)
        {
            this.receivedTcs.SetResult(new(NetResult.Closed));
            this.receivedTcs = null;
        }

        if (this.receiveStream is not null)
        {
            this.receiveStream?.Dispose();
            this.receiveStream = null;
        }
    }

    internal void SetState_Receiving(int totalGene)
    {// Since it's called immediately after the object's creation, 'lock(this.syncObject)' is probably not necessary.
        if (totalGene <= NetHelper.RamaGenes)
        {
            this.Mode = NetTransmissionMode.Rama;
        }
        else
        {
            this.Mode = NetTransmissionMode.Block;

            this.genes = new();
            this.genes.DataPositionListChain.Resize(totalGene);
            for (var i = 0; i < totalGene; i++)
            {
                var gene = new ReceiveGene(this);
                gene.Goshujin = this.genes;
                this.genes.DataPositionListChain.Add(gene);
            }
        }

        this.totalGene = totalGene;
    }

    internal void SetState_ReceivingStream(int totalGene)
    {// Since it's called immediately after the object's creation, 'lock(this.syncObject)' is probably not necessary.
        this.Mode = NetTransmissionMode.Stream;
        this.totalGene = totalGene;
    }

    internal void ProcessReceive_Gene(/*int geneSerial, */int dataPosition, ByteArrayPool.MemoryOwner toBeShared)
    {// this.Mode == NetTransmissionMode.Rama or NetTransmissionMode.Block or NetTransmissionMode.Stream
        var geneSerial = dataPosition;
        var completeFlag = false;
        uint dataKind = 0;
        ulong dataId = 0;
        ByteArrayPool.MemoryOwner owner = default;
        lock (this.syncObject)
        {
            if (this.Mode == NetTransmissionMode.Disposed)
            {// The case that the ACK has not arrived after the receive transmission was disposed.
                this.Connection.ConnectionTerminal.AckQueue.AckBlock(this.Connection, this, geneSerial);
                return;
            }
            else if (this.Mode == NetTransmissionMode.Initial)
            {// The packet must be discarded since the first packet has not been received and the receiving mode is unknown.
                return;
            }

            if (this.Mode == NetTransmissionMode.Rama)
            {// Single send/recv
                if (geneSerial == 0)
                {
                    this.gene0 ??= new(this);
                    this.gene0.SetRecv(toBeShared);
                }
                else if (geneSerial == 1)
                {
                    this.gene1 ??= new(this);
                    this.gene1.SetRecv(toBeShared);
                }
                else if (geneSerial == 2)
                {
                    this.gene2 ??= new(this);
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
            else if (this.Mode == NetTransmissionMode.Block &&
                this.genes is not null)
            {// Multiple send/recv
                var chain = this.genes.DataPositionListChain;
                if (chain.Get(dataPosition) is { } gene)
                {
                    gene.SetRecv(toBeShared);

                    if (this.maxReceivedPosition <= dataPosition)
                    {
                        if (this.maxReceivedPosition == dataPosition)
                        {
                            this.maxReceivedPosition++;
                        }

                        while (chain.Get(this.maxReceivedPosition) is { } g && g.IsReceived)
                        {
                            this.maxReceivedPosition++;
                        }
                    }

                    if (this.maxReceivedPosition >= this.totalGene)
                    {
                        completeFlag = true;
                    }
                }
            }

            if (completeFlag)
            {// Complete
                this.ProcessReceive_GeneComplete(out dataKind, out dataId, out owner);
            }
        }

        // Send Ack
        if (this.Mode == NetTransmissionMode.Rama)
        {// Fast Ack
            if (completeFlag)
            {
                if (this.Connection.Agreement.MaxTransmissions < 10)
                {// Instant
                    this.Connection.Logger.TryGet(LogLevel.Debug)?.Log($"{this.Connection.ConnectionIdText} Send Instant Ack {this.totalGene}");

                    Span<byte> ackFrame = stackalloc byte[2 + 4 + 4];
                    var span = ackFrame;
                    BitConverter.TryWriteBytes(span, (ushort)FrameType.Ack);
                    span = span.Slice(sizeof(ushort));
                    BitConverter.TryWriteBytes(span, (int)-1);
                    span = span.Slice(sizeof(int));
                    BitConverter.TryWriteBytes(span, this.TransmissionId);
                    span = span.Slice(sizeof(uint));

                    Debug.Assert(span.Length == 0);
                    this.Connection.SendPriorityFrame(ackFrame);
                }
                else
                {// Defer
                    // this.Connection.Logger.TryGet(LogLevel.Debug)?.Log($"{this.Connection.ConnectionIdText} Send Ack 0 - {this.totalGene}");

                    this.Connection.ConnectionTerminal.AckQueue.AckRama(this.Connection, this);
                }
            }
        }
        else
        {// Ack (TransmissionId, GenePosition)
            this.Connection.ConnectionTerminal.AckQueue.AckBlock(this.Connection, this, geneSerial);
        }

        if (completeFlag)
        {// Receive complete
            TaskCompletionSource<NetResponse>? receivedTcs;
            ReceiveStream? receiveStream;

            lock (this.syncObject)
            {
                receivedTcs = this.receivedTcs;
                this.receivedTcs = default;
                receiveStream = this.receiveStream;
                this.receiveStream = default;

                // this.Goshujin = null; // -> this.Connection.RemoveTransmission(this);
                this.DisposeInternal();
            }

            this.Connection.RemoveTransmission(this);

            if (owner.IsRent)
            {
                if (this.Connection is ServerConnection serverConnection)
                {// InvokeServer: Connection, NetTransmission, Owner
                    var connectionContext = serverConnection.ConnectionContext;
                    var transmissionContext = new TransmissionContext(connectionContext, this.TransmissionId, dataKind, dataId, owner.IncrementAndShare());
                    connectionContext.InvokeSync(transmissionContext);
                }

                receivedTcs?.SetResult(new(NetResult.Success, dataId, owner.IncrementAndShare(), 0));
                owner.Return();
            }
        }
    }

    internal void ProcessReceive_GeneComplete(out uint dataKind, out ulong dataId, out ByteArrayPool.MemoryOwner toBeMoved)
    {// lock (this.syncObject)
        if (this.genes is null)
        {// Single send/recv
            if (this.totalGene == 0)
            {
                dataKind = 0;
                dataId = 0;
                toBeMoved = default;
            }
            else
            {
                var span = this.gene0!.Packet.Span;
                dataKind = BitConverter.ToUInt32(span);
                span = span.Slice(sizeof(uint));
                dataId = BitConverter.ToUInt64(span);

                var firstPacket = this.gene0!.Packet.Slice(12);
                var length = firstPacket.Span.Length;
                if (this.totalGene == 1)
                {
                    toBeMoved = firstPacket.IncrementAndShare();
                }
                else if (this.totalGene == 2)
                {
                    length += this.gene1!.Packet.Span.Length;
                    toBeMoved = ByteArrayPool.Default.Rent(length).ToMemoryOwner(0, length);

                    span = toBeMoved.Span;
                    firstPacket.Span.CopyTo(span);
                    span = span.Slice(firstPacket.Span.Length);
                    this.gene1!.Packet.Span.CopyTo(span);
                }
                else if (this.totalGene == 3)
                {
                    length += this.gene1!.Packet.Span.Length;
                    length += this.gene2!.Packet.Span.Length;
                    toBeMoved = ByteArrayPool.Default.Rent(length).ToMemoryOwner(0, length);

                    span = toBeMoved.Span;
                    firstPacket.Span.CopyTo(span);
                    span = span.Slice(firstPacket.Span.Length);
                    this.gene1!.Packet.Span.CopyTo(span);
                    span = span.Slice(this.gene1!.Packet.Span.Length);
                    this.gene2!.Packet.Span.CopyTo(span);
                }
                else
                {
                    toBeMoved = default;
                }
            }

            return;
        }
        else
        {// Multiple send/recv
            // First
            var firstGene = this.genes.DataPositionListChain.Get(0);
            if (firstGene is null)
            {
                goto Abort;
            }

            var span = firstGene.Packet.Span;
            dataKind = BitConverter.ToUInt32(span);
            span = span.Slice(sizeof(uint));
            dataId = BitConverter.ToUInt64(span);

            var firstSpan = firstGene.Packet.Slice(12).Span;
            var length = firstSpan.Length;

            // Last
            var lastGene = this.genes.DataPositionListChain.Get(this.totalGene - 1);
            if (lastGene is null)
            {
                goto Abort;
            }

            length += (FollowingGeneFrame.MaxGeneLength * (this.totalGene - 2)) + lastGene.Packet.Span.Length;
            toBeMoved = ByteArrayPool.Default.Rent(length).ToMemoryOwner(0, length);
            span = toBeMoved.Span;

            firstSpan.CopyTo(span);
            span = span.Slice(firstSpan.Length);
            for (var i = 1; i < this.totalGene; i++)
            {
                var gene = this.genes.DataPositionListChain.Get(i);
                if (gene is null)
                {
                    toBeMoved.Return();
                    goto Abort;
                }

                var src = gene.Packet.Span;
                src.CopyTo(span);
                span = span.Slice(src.Length);
            }

            Debug.Assert(span.Length == 0);
            return;
        }

Abort:
        dataKind = 0;
        dataId = 0;
        toBeMoved = default;
    }
}
