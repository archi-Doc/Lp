// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Arc.Collections;
using Netsphere.Block;
using Netsphere.Net;
using Netsphere.Packet;

#pragma warning disable SA1202
#pragma warning disable SA1214
#pragma warning disable SA1401

namespace Netsphere;

// byte[32] Key, byte[16] Iv
internal readonly record struct Embryo(ulong Salt, byte[] Key, byte[] Iv);

public abstract class Connection : IDisposable
{
    private const int LowerRttLimit = 1_000; // 1ms
    private const int UpperRttLimit = 1_000_000; // 1000ms

    public enum ConnectMode
    {
        ReuseClosed,
        Shared,
        NoReuse,
    }

    public enum ConnectionState
    {
        Created,
        Open,
        Closed,
        Disposed,
    }

    public Connection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetEndPoint endPoint)
    {
        this.NetBase = connectionTerminal.NetBase;
        this.Logger = this.NetBase.UnitLogger.GetLogger(this.GetType());
        this.PacketTerminal = packetTerminal;
        this.ConnectionTerminal = connectionTerminal;
        this.ConnectionId = connectionId;
        this.EndPoint = endPoint;
    }

    #region FieldAndProperty

    public CancellationToken CancellationToken => this.NetBase.CancellationToken;

    public NetBase NetBase { get; }

    public ConnectionTerminal ConnectionTerminal { get; }

    public PacketTerminal PacketTerminal { get; }

    public ulong ConnectionId { get; }

    public string ConnectionIdText
        => ((ushort)this.ConnectionId).ToString("x4");

    public NetEndPoint EndPoint { get; }

    public ConnectionAgreementBlock Agreement { get; private set; } = ConnectionAgreementBlock.Default;

    public abstract ConnectionState State { get; }

    public abstract bool IsClient { get; }

    public abstract bool IsServer { get; }

    public bool IsOpen
        => this.State == ConnectionState.Open;

    public bool IsClosedOrDisposed
        => this.State == ConnectionState.Closed || this.State == ConnectionState.Disposed;

    public int SmoothedRtt
        => this.smoothedRtt;

    public int RetransmissionTimeout
        => this.smoothedRtt + Math.Max(this.rttvar * 4, 1_000) + NetConstants.AckDelayMics; // 1ms

    public int SentCount
        => this.sentCount;

    public int ResendCount
        => this.resendCount;

    public double DeliveryRatio
    {
        get
        {
            var total = this.sentCount + this.resendCount;
            return total == 0 ? 1.0d : (this.sentCount / (double)total);
        }
    }

    internal ILogger Logger { get; }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
    internal long closedSystemMics;
    internal long responseSystemMics; // When any packet, including an Ack, is received, it's updated to the latest time.
    internal ICongestionControl? CongestionControl; // ConnectionTerminal.flowControls.SyncObject
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

    private Embryo embryo;

    // lock (this.syncAes)
    private readonly object syncAes = new();
    private Aes? aes0;
    private Aes? aes1;

    private SendTransmission.GoshujinClass sendTransmissions = new(); // lock (this.sendTransmissions.SyncObject)
    private UnorderedLinkedList<SendTransmission> sendList = new(); // lock (this.ConnectionTerminal.SyncSend)

    // ReceiveTransmissionCode, lock (this.receiveTransmissions.SyncObject)
    private ReceiveTransmission.GoshujinClass receiveTransmissions = new();
    private UnorderedLinkedList<ReceiveTransmission> receiveReceivedList = new();
    private UnorderedLinkedList<ReceiveTransmission> receiveDisposedList = new();

    // RTT
    private int minRtt; // Minimum rtt (mics)
    private int smoothedRtt; // Smoothed rtt (mics)
    private int rttvar; // Rtt variation (mics)
    private int sentCount;
    private int resendCount;

    // Ack
    internal long AckMics; // lock(AckBuffer.syncObject)
    internal Queue<ulong>? AckQueue; // lock(AckBuffer.syncObject)

    #endregion

    public void CreateFlowControl()
    {
        if (this.flowControl is null)
        {
            this.ConnectionTerminal.CreateFlowControl(this);
        }
    }

    public void ResetFlowControl()
    {
        if (this.flowControl is not null)
        {
            this.ConnectionTerminal.RemoveFlowControl(this);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddSend(SendTransmission transmission)
    {
        var list = this.ConnectionTerminal.SendList;
        lock (this.ConnectionTerminal.SyncSend)
        {
            if (this.sendNode is null)
            {
                this.sendNode = list.AddLast(this);
            }

            if (transmission.SendNode is null)
            {
                transmission.SendNode = this.sendList.AddLast(transmission);
            }
        }
    }

    public void Close()
        => this.Dispose();

    /*internal SendTransmission? TryCreateSendTransmission()
    {
        lock (this.sendTransmissions.SyncObject)
        {
            if (this.sendTransmissions.Count >= this.Agreement.MaxTransmissions)
            {
                return default;
            }

            uint transmissionId;
            do
            {
                transmissionId = RandomVault.Pseudo.NextUInt32();
            }
            while (transmissionId == 0 || this.sendTransmissions.TransmissionIdChain.ContainsKey(transmissionId));

            var sendTransmission = new SendTransmission(this, transmissionId);
            sendTransmission.Goshujin = this.sendTransmissions;
            return sendTransmission;
        }
    }*/

    internal SendTransmission? TryCreateSendTransmission(uint transmissionId)
    {
        lock (this.sendTransmissions.SyncObject)
        {
            if (this.IsClosedOrDisposed)
            {
                return default;
            }

            /* To maintain consistency with the number of SendTransmission on the client side, limit the number of ReceiveTransmission in ProcessReceive_FirstGene().
            if (this.sendTransmissions.Count >= this.Agreement.MaxTransmissions)
            {
                return default;
            }*/

            if (transmissionId == 0 || this.sendTransmissions.TransmissionIdChain.ContainsKey(transmissionId))
            {
                return default;
            }

            var sendTransmission = new SendTransmission(this, transmissionId);
            sendTransmission.Goshujin = this.sendTransmissions;
            return sendTransmission;
        }
    }

    internal async ValueTask<SendTransmissionAndTimeout> TryCreateSendTransmission(TimeSpan timeout)
    {
Retry:
        if (this.CancellationToken.IsCancellationRequested ||
            timeout < TimeSpan.Zero)
        {
            return default;
        }

        lock (this.sendTransmissions.SyncObject)
        {
            if (this.IsClosedOrDisposed)
            {
                return default;
            }

            if (this.sendTransmissions.Count >= this.Agreement.MaxTransmissions)
            {
                goto Wait;
            }

            uint transmissionId;
            do
            {
                transmissionId = RandomVault.Pseudo.NextUInt32();
            }
            while (transmissionId == 0 || this.sendTransmissions.TransmissionIdChain.ContainsKey(transmissionId));

            var sendTransmission = new SendTransmission(this, transmissionId);
            sendTransmission.Goshujin = this.sendTransmissions;
            return new(sendTransmission, timeout);
        }

Wait:
        try
        {
            await Task.Delay(NetConstants.CreateTransmissionDelay, this.CancellationToken).ConfigureAwait(false);
        }
        catch
        {// Cancelled
            return default;
        }

        timeout -= NetConstants.CreateTransmissionDelay;
        goto Retry;
    }

    internal ReceiveTransmission? TryCreateReceiveTransmission(uint transmissionId, TaskCompletionSource<NetResponse>? receivedTcs, ReceiveStream? receiveStream)
    {
        lock (this.receiveTransmissions.SyncObject)
        {
            Debug.Assert(this.receiveTransmissions.Count == (this.receiveReceivedList.Count + this.receiveDisposedList.Count));

            // Release receive transmissions that have elapsed a certain time after being disposed.
            var currentMics = Mics.FastSystem;
            while (this.receiveDisposedList.First is { } node)
            {
                var transmission = node.Value;
                if (currentMics < transmission.ReceivedDisposedMics + NetConstants.TransmissionDisposalMics)
                {
                    break;
                }

                node.List.Remove(node);
                transmission.ReceivedDisposedNode = null;
                transmission.Goshujin = null;
            }

            // Release receive transmissions that have elapsed a certain time since the last data reception.
            while (this.receiveReceivedList.First is { } node)
            {
                var transmission = node.Value;
                if (currentMics < transmission.ReceivedDisposedMics + NetConstants.TransmissionTimeoutMics)
                {
                    break;
                }

                node.List.Remove(node);
                transmission.DisposeTransmission();
                transmission.ReceivedDisposedNode = null;
                transmission.Goshujin = null;
            }

            if (this.IsClosedOrDisposed)
            {
                return default;
            }

            if (this.receiveTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out var receiveTransmission))
            {
                return default;
            }

            receiveTransmission = new ReceiveTransmission(this, transmissionId, receivedTcs, receiveStream);
            receiveTransmission.ReceivedDisposedMics = currentMics;
            receiveTransmission.ReceivedDisposedNode = this.receiveReceivedList.AddLast(receiveTransmission);
            receiveTransmission.Goshujin = this.receiveTransmissions;
            return receiveTransmission;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool RemoveTransmission(SendTransmission transmission)
    {
        lock (this.sendTransmissions.SyncObject)
        {
            if (transmission.Goshujin == this.sendTransmissions)
            {
                transmission.Goshujin = null;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool RemoveTransmission(ReceiveTransmission transmission)
    {
        lock (this.receiveTransmissions.SyncObject)
        {
            if (transmission.Goshujin == this.receiveTransmissions)
            {
                transmission.ReceivedDisposedMics = Mics.FastSystem; // Disposed mics
                if (transmission.ReceivedDisposedNode is { } node)
                {// ReceivedList -> DisposedList
                    node.List.Remove(node);
                    transmission.ReceivedDisposedNode = this.receiveDisposedList.AddLast(transmission);
                    Debug.Assert(transmission.ReceivedDisposedNode.List != null);
                }
                else
                {// -> DisposedList
                    transmission.ReceivedDisposedNode = this.receiveDisposedList.AddLast(transmission);
                }

                // transmission.Goshujin = null; // Delay the release to return ACK even after the receive transmission has ended.
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    internal void Initialize(ConnectionAgreementBlock agreement, Embryo embryo)
    {
        this.Agreement = agreement;
        this.embryo = embryo;
    }

    internal void AddRtt(int rttMics)
    {
        if (rttMics < LowerRttLimit)
        {
            rttMics = LowerRttLimit;
        }
        else if (rttMics > UpperRttLimit)
        {
            rttMics = UpperRttLimit;
        }

        if (this.minRtt == 0)
        {// Initial
            this.minRtt = rttMics;
            this.smoothedRtt = rttMics;
            this.rttvar = rttMics >> 1;
        }
        else
        {// Update
            if (this.minRtt > rttMics)
            {// minRtt is greater then the latest rtt.
                this.minRtt = rttMics;
            }

            var adjustedRtt = rttMics; // - ackDelay
            this.smoothedRtt = ((this.smoothedRtt * 7) >> 3) + (adjustedRtt >> 3);
            var rttvarSample = Math.Abs(this.smoothedRtt - adjustedRtt);
            this.rttvar = ((this.rttvar * 3) >> 2) + (rttvarSample >> 2);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ReportResend()
    {
        this.resendCount++; // Not thread-safe, though it doesn't matter.
        // this.AddRtt(this.smoothedRtt * 2); // tempcode
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementSentCount()
    {
        this.sentCount++; // Not thread-safe, though it doesn't matter.
    }

    internal void SendPriorityFrame(scoped Span<byte> frame)
    {// Close, Ack
        if (!this.CreatePacket(frame, out var owner))
        {
            return;
        }

        this.PacketTerminal.AddSendPacket(this.EndPoint.EndPoint, owner, default);
    }

    internal void SendCloseFrame() // Close
        => this.SendPriorityFrame([]);

    internal ProcessSendResult ProcessSend(NetSender netSender)
    {// lock (this.ConnectionTerminal.SyncSend). true: remaining genes
        if (this.State == ConnectionState.Closed ||
            this.State == ConnectionState.Disposed)
        {// Connection closed
            return ProcessSendResult.Complete;
        }

        while (this.sendList.First is { } node)
        {
            var transmission = node.Value;
            Debug.Assert(transmission.SendNode == node);

            //Congestion control

            var result = transmission.ProcessSend(netSender, this.flowControl);
            if (result == ProcessSendResult.Complete)
            {// No transmission to send.
                this.sendList.Remove(node);
                transmission.SendNode = default;
            }
            else if (result == ProcessSendResult.Remaining)
            {// Remaining
                this.sendList.MoveToLast(node);
            }
            else
            {
            }
        }

        if ()

        return this.sendList.Count > 0;
    }

    internal void ProcessReceive(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {// endPoint: Checked
        if (this.State == ConnectionState.Disposed)
        {
            return;
        }

        // PacketHeaderCode
        var span = toBeShared.Span;

        var salt = BitConverter.ToUInt32(span); // Salt
        span = span.Slice(6);

        var packetType = (PacketType)BitConverter.ToUInt16(span); // PacketType
        span = span.Slice(10);

        if (span.Length == 0)
        {// Close frame
            this.ConnectionTerminal.CloseInternal(this, false);
            return;
        }

        if (packetType == PacketType.Encrypted || packetType == PacketType.EncryptedResponse)
        {
            if (!this.TryDecryptCbc(salt, span, PacketPool.MaxPacketSize - PacketHeader.Length, out var written))
            {
                return;
            }

            if (written < 2)
            {
                return;
            }

            this.responseSystemMics = Mics.FastSystem;

            var owner = toBeShared.Slice(PacketHeader.Length + 2, written - 2);
            var frameType = (FrameType)BitConverter.ToUInt16(span); // FrameType
            if (frameType == FrameType.Ack)
            {// Ack
                this.ProcessReceive_Ack(endPoint, owner);
            }
            else if (frameType == FrameType.FirstGene)
            {// FirstGene
                this.ProcessReceive_FirstGene(endPoint, owner);
            }
            else if (frameType == FrameType.FollowingGene)
            {// FollowingGene
                this.ProcessReceive_FollowingGene(endPoint, owner);
            }
        }
    }

    internal void ProcessReceive_Ack(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
    {// uint TransmissionId, ushort NumberOfPairs, { int StartGene, int EndGene } x pairs
        var span = toBeShared.Span;
        lock (this.sendTransmissions.SyncObject)
        {
            while (span.Length >= 6)
            {
                var transmissionId = BitConverter.ToUInt32(span);
                span = span.Slice(sizeof(uint));
                var numberOfPairs = BitConverter.ToUInt16(span);
                span = span.Slice(sizeof(ushort));

                var length = numberOfPairs * 8;
                if (span.Length < length)
                {
                    return;
                }

                if (!this.sendTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out var transmission))
                {
                    span = span.Slice(length);
                    continue;
                }

                transmission.ProcessReceive_Ack(span);
                span = span.Slice(length);
            }
        }
    }

    internal void ProcessReceive_FirstGene(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
    {// First gene
        var span = toBeShared.Span;
        if (span.Length < FirstGeneFrame.LengthExcludingFrameType)
        {
            return;
        }

        // FirstGeneFrameCode
        var transmissionMode = BitConverter.ToUInt16(span);
        span = span.Slice(sizeof(ushort)); // 2
        var transmissionId = BitConverter.ToUInt32(span);
        span = span.Slice(sizeof(uint)); // 4
        var rttHint = BitConverter.ToInt32(span);
        span = span.Slice(sizeof(int)); // 4
        var totalGenes = BitConverter.ToInt32(span);
        span = span.Slice(sizeof(int)); // 4

        if (rttHint > 0)
        {
            this.AddRtt(rttHint);
        }

        ReceiveTransmission? transmission;
        lock (this.receiveTransmissions.SyncObject)
        {
            if (this.IsClient)
            {// Client side
                if (!this.receiveTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out transmission))
                {// On the client side, it's necessary to create ReceiveTransmission in advance.
                    return;
                }
                else if (transmission.Mode != NetTransmissionMode.Initial)
                {// Processing the first packet is limited to the initial state, as the state gets cleared.
                    return;
                }

                transmission.SetState_Receiving(totalGenes);
            }
            else
            {// Server side
                if (this.receiveTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out transmission))
                {// The same TransmissionId already exists.
                    this.ConnectionTerminal.AckBuffer.Add(this, transmissionId, 0); // Resend the ACK in case it was not received.
                    return;
                }

                // New transmission
                if (this.receiveReceivedList.Count >= this.Agreement.MaxTransmissions)
                {// Maximum number reached.
                    return;
                }

                if (transmissionMode == 0 && totalGenes <= this.Agreement.MaxBlockGenes)
                {// Block mode
                    transmission = new(this, transmissionId, default, default);
                    transmission.SetState_Receiving(totalGenes);
                }
                else if (transmissionMode == 1 && totalGenes < this.Agreement.MaxStreamGenes)
                {// Stream mode
                    transmission = new(this, transmissionId, default, default);
                    transmission.SetState_ReceivingStream(totalGenes);
                }
                else
                {
                    return;
                }

                transmission.Goshujin = this.receiveTransmissions;
                transmission.ReceivedDisposedMics = Mics.FastSystem; // Received mics
                transmission.ReceivedDisposedNode = this.receiveReceivedList.AddLast(transmission);
            }
        }

        transmission.ProcessReceive_Gene(0, 0, toBeShared.Slice(14)); // FirstGeneFrameCode
    }

    internal void ProcessReceive_FollowingGene(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
    {// Following gene
        var span = toBeShared.Span;
        if (span.Length < FollowingGeneFrame.LengthExcludingFrameType)
        {
            return;
        }

        var transmissionId = BitConverter.ToUInt32(span);
        span = span.Slice(sizeof(uint));
        var geneSerial = BitConverter.ToInt32(span);
        span = span.Slice(sizeof(int));
        if (geneSerial == 0)
        {
            return;
        }

        var dataPosition = BitConverter.ToInt32(span);

        ReceiveTransmission? transmission;
        lock (this.receiveTransmissions.SyncObject)
        {
            if (!this.receiveTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out transmission))
            {// No transmission
                return;
            }

            // ReceiveTransmissionsCode
            if (transmission.ReceivedDisposedNode is { } node &&
                node.List == this.receiveReceivedList)
            {
                transmission.ReceivedDisposedMics = Mics.FastSystem; // Received mics
                this.receiveReceivedList.MoveToLast(node);//check
                /*this.receiveReceivedList.Remove(node);
                // this.receiveReceivedList.AddLast(node); // The node has been disposed and therefore cannot be reused.
                transmission.ReceivedDisposedNode = this.receiveReceivedList.AddLast(transmission);*/
            }
        }

        transmission.ProcessReceive_Gene(geneSerial, dataPosition, toBeShared.Slice(FollowingGeneFrame.LengthExcludingFrameType));
    }

    internal bool CreatePacket(scoped Span<byte> frame, out ByteArrayPool.MemoryOwner owner)
    {
        if (frame.Length > PacketHeader.MaxFrameLength)
        {
            owner = default;
            return false;
        }

        var packetType = this is ClientConnection ? PacketType.Encrypted : PacketType.EncryptedResponse;
        var arrayOwner = PacketPool.Rent();
        var span = arrayOwner.ByteArray.AsSpan();
        var salt = RandomVault.Pseudo.NextUInt32();

        // PacketHeaderCode
        BitConverter.TryWriteBytes(span, salt); // Salt
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)this.EndPoint.Engagement); // Engagement
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, (ushort)packetType); // PacketType
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.ConnectionId); // Id
        span = span.Slice(sizeof(ulong));

        int written = 0;
        if (frame.Length > 0)
        {
            if (!this.TryEncryptCbc(salt, frame, arrayOwner.ByteArray.AsSpan(PacketHeader.Length), out written))
            {
                owner = default;
                return false;
            }
        }

        owner = arrayOwner.ToMemoryOwner(0, PacketHeader.Length + written);
        return true;
    }

    internal void CreatePacket(scoped Span<byte> frameHeader, scoped Span<byte> frameContent, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert((frameHeader.Length + frameContent.Length) <= PacketHeader.MaxFrameLength);

        var packetType = this is ClientConnection ? PacketType.Encrypted : PacketType.EncryptedResponse;
        var arrayOwner = PacketPool.Rent();
        var span = arrayOwner.ByteArray.AsSpan();
        var salt = RandomVault.Pseudo.NextUInt32();

        // PacketHeaderCode
        BitConverter.TryWriteBytes(span, salt); // Salt
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)this.EndPoint.Engagement); // Engagement
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, (ushort)packetType); // PacketType
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.ConnectionId); // Id
        span = span.Slice(sizeof(ulong));

        frameHeader.CopyTo(span);
        span = span.Slice(frameHeader.Length);
        frameContent.CopyTo(span);
        span = span.Slice(frameContent.Length);

        this.TryEncryptCbc(salt, arrayOwner.ByteArray.AsSpan(PacketHeader.Length, frameHeader.Length + frameContent.Length), PacketPool.MaxPacketSize - PacketHeader.Length, out var written);
        owner = arrayOwner.ToMemoryOwner(0, PacketHeader.Length + written);
    }

    internal void CreateAckPacket(ByteArrayPool.Owner owner, int geneLength, out int packetLength)
    {
        var packetType = this is ClientConnection ? PacketType.Encrypted : PacketType.EncryptedResponse;
        var span = owner.ByteArray.AsSpan();
        var salt = RandomVault.Pseudo.NextUInt32();

        // PacketHeaderCode
        BitConverter.TryWriteBytes(span, salt); // Salt
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)this.EndPoint.Engagement); // Engagement
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, (ushort)packetType); // PacketType
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.ConnectionId); // Id
        span = span.Slice(sizeof(ulong));

        var source = span;
        BitConverter.TryWriteBytes(span, (ushort)FrameType.Ack); // Frame type
        span = span.Slice(sizeof(ushort));

        this.TryEncryptCbc(salt, source.Slice(0, sizeof(ushort) + geneLength), PacketPool.MaxPacketSize - PacketHeader.Length, out var written);
        packetLength = PacketHeader.Length + written;
    }

    /// <inheritdoc/>
    public void Dispose()
    {// Close the connection, but defer actual disposal for reuse.
        this.ConnectionTerminal.CloseInternal(this, true);
    }

    internal void DisposeActual()
    {// lock (this.Goshujin.SyncObject)
        this.Logger.TryGet(LogLevel.Debug)?.Log($"{this.ConnectionIdText} Dispose actual, SendCloseFrame {this.State == ConnectionState.Open}");

        if (this.State == ConnectionState.Open)
        {
            this.SendCloseFrame();
        }

        lock (this.syncAes)
        {
            this.aes0?.Dispose();
            this.aes1?.Dispose();
        }

        this.CloseTransmission();
    }

    public override string ToString()
    {
        var connectionString = "Connection";
        if (this is ServerConnection)
        {
            connectionString = "Server";
        }
        else if (this is ClientConnection)
        {
            connectionString = "Client";
        }

        return $"{connectionString} Id:{(ushort)this.ConnectionId:x4}, EndPoint:{this.EndPoint.ToString()}, Delivery:{this.DeliveryRatio.ToString("F2")} ({this.SentCount}/{this.SentCount + this.ResendCount})";
    }

    internal bool TryEncryptCbc(uint salt, Span<byte> source, Span<byte> destination, out int written)
    {
        Span<byte> iv = stackalloc byte[16];
        this.embryo.Iv.CopyTo(iv);
        BitConverter.TryWriteBytes(iv, salt);

        var aes = this.RentAes();
        var result = aes.TryEncryptCbc(source, iv, destination, out written, PaddingMode.PKCS7);
        this.ReturnAes(aes);
        return result;
    }

    internal bool TryEncryptCbc(uint salt, Span<byte> span, int spanMax, out int written)
    {
        Span<byte> iv = stackalloc byte[16];
        this.embryo.Iv.CopyTo(iv);
        BitConverter.TryWriteBytes(iv, salt);

        var aes = this.RentAes();
        var result = aes.TryEncryptCbc(span, iv, MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), spanMax), out written, PaddingMode.PKCS7);
        this.ReturnAes(aes);
        return result;
    }

    internal bool TryDecryptCbc(uint salt, Span<byte> span, int spanMax, out int written)
    {
        Span<byte> iv = stackalloc byte[16];
        this.embryo.Iv.CopyTo(iv);
        BitConverter.TryWriteBytes(iv, salt);

        var aes = this.RentAes();
        var result = aes.TryDecryptCbc(span, iv, MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), spanMax), out written, PaddingMode.PKCS7);
        this.ReturnAes(aes);
        return result;
    }

    internal void CloseTransmission()
    {
        lock (this.sendTransmissions.SyncObject)
        {
            foreach (var x in this.sendTransmissions)
            {
                x.DisposeTransmission();
                x.Goshujin = null;
            }

            // Since it's within a lock statement, manually clear it.
            this.sendTransmissions.TransmissionIdChain.Clear();
        }

        lock (this.receiveTransmissions.SyncObject)
        {
            foreach (var x in this.receiveTransmissions)
            {
                x.DisposeTransmission();

                if (x.ReceivedDisposedNode is { } node)
                {
                    node.List.Remove(node);
                    x.ReceivedDisposedNode = null;
                }

                x.Goshujin = null;
            }

            // Since it's within a lock statement, manually clear it.
            // ReceiveTransmissionsCode
            this.receiveTransmissions.TransmissionIdChain.Clear();
            this.receiveReceivedList.Clear();
            this.receiveDisposedList.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Aes RentAes()
    {
        lock (this.syncAes)
        {
            Aes aes;
            if (this.aes0 is not null)
            {
                aes = this.aes0;
                this.aes0 = this.aes1;
                this.aes1 = default;
                return aes;
            }
            else
            {
                aes = Aes.Create();
                aes.KeySize = 256;
                aes.Key = this.embryo.Key;
                return aes;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnAes(Aes aes)
    {
        lock (this.syncAes)
        {
            if (this.aes0 is null)
            {
                this.aes0 = aes;
                return;
            }
            else if (this.aes1 is null)
            {
                this.aes1 = aes;
                return;
            }
            else
            {
                aes.Dispose();
            }
        }
    }
}
