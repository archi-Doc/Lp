// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class ReceiveTransmission : IDisposable
{
    public ReceiveTransmission(Connection connection, uint transmissionId, bool invokeServer)
    {
        this.Connection = connection;
        this.TransmissionId = transmissionId;
        this.InvokeServer = invokeServer;
    }

    #region FieldAndProperty

    public Connection Connection { get; }

    public bool InvokeServer { get; }

    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint TransmissionId { get; }

    public NetTransmissionMode Mode { get; private set; } // lock (this.syncObject)

    private readonly object syncObject = new();
    private int totalGene;
    private uint maxReceived;
    private TaskCompletionSource<NetResponse>? tcs;
    private ReceiveGene? gene0; // Gene 0
    private ReceiveGene? gene1; // Gene 1
    private ReceiveGene? gene2; // Gene 2
    private ReceiveGene.GoshujinClass? genes; // Multiple genes

    #endregion

    public void Dispose()
    {
        this.Connection.RemoveTransmission(this);
        this.DisposeInternal();
    }

    internal void SetState_Receiving(int totalGene)
    {
        this.Mode = NetTransmissionMode.Block;
        this.totalGene = totalGene;
    }

    internal void SetState_ReceivingStream(int totalGene)
    {
        this.Mode = NetTransmissionMode.Stream;
        this.totalGene = totalGene;
    }

    internal void DisposeInternal()
    {
        TaskCompletionSource<NetResponse>? tcs;

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

    internal void ProcessReceive_Gene(int genePosition, ByteArrayPool.MemoryOwner toBeShared)
    {
        var completeFlag = false;
        uint primaryId = 0;
        ulong secondaryId = 0;
        ByteArrayPool.MemoryOwner owner = default;
        lock (this.syncObject)
        {
            if (this.Mode == NetTransmissionMode.Rama)
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
            else if (genePosition < this.totalGene)
            {// Multiple send/recv
            }

            if (completeFlag)
            {// Complete
                this.ProcessReceive_GeneComplete(out primaryId, out secondaryId, out owner);
            }
        }

        // Send Ack
        if (this.Mode == NetTransmissionMode.Rama)
        {// Fast Ack
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
            this.Connection.AddAck(this.TransmissionId, genePosition);
        }

        if (completeFlag)
        {// Receive complete
            if (this.InvokeServer)
            {// Server: Connection, NetTransmission, Owner
                var param = new ServerInvocationParam(this.Connection, this, primaryId, secondaryId, owner);
                Console.WriteLine(owner.Span.Length);
            }
            else
            {// Client
                this.Dispose();

                if (this.tcs is not null)
                {
                    this.tcs.SetResult(new(NetResult.Success, owner, 0));
                }
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
}
