// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Netsphere.Packet;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class NetTransmission : NetStream, IDisposable
{
    /* State transitions
     *  SendAndReceiveAsync (Client) : Initial -> Sending -> Receiving -> Disposed
     *  SendAsync                   (Client) : Initial -> Sending -> tcs / Disposed
     *  (Server) : Initial -> Receiving -> Received -> Disposed
     *  (Server) : Initial -> Receiving -> Received -> Sending -> tcs / Disposed
     */
    public enum TransmissionState
    {
        Initial,
        Sending,
        SendingStream,
        Receiving,
        ReceivingStream,
        Received,
        Disposed,
    }

    public enum TransmissionMode
    {
        Invalid,
        Rama,
        Block,
        Stream,
    }

    // [Link(Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false)]
    // [Link(Name = "ResendQueue", Type = ChainType.QueueList, AutoLink = false)]
    public NetTransmission(Connection connection, bool isClient, uint transmissionId)
    {
        this.Connection = connection;
        this.TransmissionId = transmissionId;

        this.IsClient = isClient;
        this.State = TransmissionState.Initial;
    }

    #region FieldAndProperty

    public Connection Connection { get; }

    public bool IsClient { get; }

    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint TransmissionId { get; }

    public TransmissionState State { get; private set; } // lock (this.syncObject)

    public FlowControl FlowControl => this.flowControl ?? FlowControl.Default;

    // internal object SyncObject => this.syncObject;

    private readonly object syncObject = new();
    private FlowControl? flowControl;
    private uint totalGene;
    private TaskCompletionSource<NetResponse>? tcs;
    private NetGene? gene0; // Gene 0
    private NetGene? gene1; // Gene 1
    private NetGene? gene2; // Gene 2
    private NetGene.GoshujinClass? genes; // Multiple genes

    #endregion

    Task<NetResult> NetStream.SendAsync(Span<byte> data)
    {
        if (this.State != TransmissionState.SendingStream)
        {
            return Task.FromResult(NetResult.InvalidTransmissionState);
        }

        return Task.FromResult(NetResult.Success);
    }

    public void Dispose()
    {
        this.Connection.RemoveTransmission(this);
        this.DisposeInternal();
    }

    internal void SetState_Receiving(uint totalGene)
    {
        this.State = TransmissionState.Receiving;
        this.totalGene = totalGene;
    }

    internal void SetState_ReceivingStream(uint totalGene)
    {
        this.State = TransmissionState.ReceivingStream;
        this.totalGene = totalGene;
    }

    internal void DisposeInternal()
    {
        TaskCompletionSource<NetResponse>? tcs;

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

        tcs?.TrySetResult(new(NetResult.Closed));
    }

    internal NetResult SendBlock(uint primaryId, ulong secondaryId, ByteArrayPool.MemoryOwner block, TaskCompletionSource<NetResponse> tcs, bool requiresResponse)
    {
        var info = NetHelper.CalculateGene(block.Span.Length);

        lock (this.syncObject)
        {
            Debug.Assert(this.State == TransmissionState.Initial);

            this.State = TransmissionState.Sending;
            this.tcs = tcs;
            this.totalGene = info.NumberOfGenes;

            var span = block.Span;
            if (info.NumberOfGenes == 1)
            {// gene0
                this.gene0 = new(this);
                this.CreateFirstPacket(0, info.NumberOfGenes, primaryId, secondaryId, span, out var owner);
                this.gene0.SetSend(owner);
            }
            else if (info.NumberOfGenes == 2)
            {// gene0, gene1
                this.gene0 = new(this);
                this.CreateFirstPacket(0, info.NumberOfGenes, primaryId, secondaryId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                this.gene0.SetSend(owner);

                span = span.Slice((int)info.FirstGeneSize);
                Debug.Assert(span.Length == info.LastGeneSize);
                this.gene1 = new(this);
                this.CreateFollowingPacket(1, span, out owner);
                this.gene1.SetSend(owner);
            }
            else if (info.NumberOfGenes == 3)
            {// gene0, gene1, gene2
                this.gene0 = new(this);
                this.CreateFirstPacket(0, info.NumberOfGenes, primaryId, secondaryId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                this.gene0.SetSend(owner);

                span = span.Slice((int)info.FirstGeneSize);
                this.gene1 = new(this);
                this.CreateFollowingPacket(1, span.Slice(0, FollowingGeneFrame.MaxGeneLength), out owner);
                this.gene1.SetSend(owner);

                span = span.Slice(FollowingGeneFrame.MaxGeneLength);
                Debug.Assert(span.Length == info.LastGeneSize);
                this.gene2 = new(this);
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
                this.genes.GenePositionListChain.Resize((int)info.NumberOfGenes);

                var firstGene = new NetGene(this);
                this.CreateFirstPacket(0, info.NumberOfGenes, primaryId, secondaryId, span.Slice(0, (int)info.FirstGeneSize), out var owner);
                firstGene.SetSend(owner);
                span = span.Slice((int)info.FirstGeneSize);
                firstGene.Goshujin = this.genes;
                this.genes.GenePositionListChain.Add(firstGene);

                for (uint i = 1; i < info.NumberOfGenes; i++)
                {
                    var size = (int)(i == info.NumberOfGenes - 1 ? info.LastGeneSize : FollowingGeneFrame.MaxGeneLength);
                    var gene = new NetGene(this);
                    this.CreateFollowingPacket(i, span.Slice(0, size), out owner);
                    gene.SetSend(owner);

                    span = span.Slice(size);
                    gene.Goshujin = this.genes;
                    this.genes.GenePositionListChain.Add(gene);
                }

                Debug.Assert(span.Length == 0);
            }
        }

        if (info.NumberOfGenes > NetHelper.RamaGenes)
        {// Flow control
        }
        else
        {
            this.Connection.ConnectionTerminal.RegisterSend(this);
        }

        return NetResult.Success;
    }

    internal NetResult SendStream(uint primaryId, ulong secondaryId, long size, bool requiresResponse)
    {
        var info = NetHelper.CalculateGene(size);

        lock (this.syncObject)
        {
            Debug.Assert(this.State == TransmissionState.Initial);

            if (info.NumberOfGenes > this.Connection.Agreement.MaxStreamGenes)
            {
                return NetResult.StreamSizeLimit;
            }

            this.State = TransmissionState.SendingStream;
            this.totalGene = info.NumberOfGenes;
        }

        return NetResult.Success;
    }

    internal void ProcessReceive_Gene(uint genePosition, ByteArrayPool.MemoryOwner toBeShared)
    {
        var completeFlag = false;
        uint primaryId = 0;
        ulong secondaryId = 0;
        ByteArrayPool.MemoryOwner owner = default;
        lock (this.syncObject)
        {
            if (this.State == TransmissionState.Receiving &&
                genePosition < this.totalGene)
            {// Set gene
                if (this.totalGene <= NetHelper.RamaGenes)
                {// Single send/recv
                    if (genePosition == 0)
                    {
                        this.gene0 ??= new(this);
                        this.gene0.SetRecv(toBeShared);
                    }
                    else if (genePosition == 1)
                    {
                        this.gene1 ??= new(this);
                        this.gene1.SetRecv(toBeShared);
                    }
                    else if (genePosition == 2)
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
                else
                {// Multiple send/recv
                }

                if (completeFlag)
                {// Complete
                    this.ProcessReceive_GeneComplete(out primaryId, out secondaryId, out owner);
                }
            }
        }

        if (this.totalGene <= NetHelper.RamaGenes)
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

        if (completeFlag)
        {// Receive complete
            if (this.IsClient)
            {// Client
                this.Dispose();

                if (this.tcs is not null)
                {
                    this.tcs.SetResult(new(NetResult.Success, owner, 0));
                }
            }
            else
            {// Server: Connection, NetTransmission, Owner
                var param = new ServerInvocationParam(this.Connection, this, primaryId, secondaryId, owner);
            }
        }
    }

    internal void ProcessReceive_GeneComplete(out uint primaryId, out ulong secondaryId, out ByteArrayPool.MemoryOwner toBeMoved)
    {// lock (this.syncObject)
        if (this.genes is null)
        {// Single send/recv
            if (this.totalGene == 0)
            {
                primaryId = 0;
                secondaryId = 0;
                toBeMoved = default;
            }
            else
            {
                var span = this.gene0!.Packet.Span;
                primaryId = BitConverter.ToUInt32(span);
                span = span.Slice(sizeof(uint));
                secondaryId = BitConverter.ToUInt64(span);

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
        }
        else
        {// Multiple send/recv
            primaryId = 0;
            secondaryId = 0;
            toBeMoved = default;
        }
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
            this.Dispose();
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
            {// Rama
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

    private void CreateFirstPacket(ushort transmissionMode, uint totalGene, uint primaryId, ulong secondaryId, Span<byte> block, out ByteArrayPool.MemoryOwner owner)
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
