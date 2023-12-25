// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

    public FlowControl FlowControl
        => this.flowControl ?? this.ConnectionTerminal.SharedFlowControl;

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

    internal ILogger Logger { get; }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
    internal long closedSystemMics;
    internal long responseSystemMics; // When any packet, including an Ack, is received, it's updated to the latest time.
    internal FlowControl? flowControl; // ConnectionTerminal.flowControls.SyncObject
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

    private Embryo embryo;

    // lock (this.syncAes)
    private readonly object syncAes = new();
    private Aes? aes0;
    private Aes? aes1;

    private SendTransmission.GoshujinClass sendTransmissions = new(); // lock (this.sendTransmissions.SyncObject)
    private ReceiveTransmission.GoshujinClass receiveTransmissions = new(); // lock (this.receiveTransmissions.SyncObject)

    // RTT
    private int minRtt; // Minimum rtt (mics)
    private int smoothedRtt; // Smoothed rtt (mics)
    private int rttvar; // Rtt variation (mics)

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

            if (this.sendTransmissions.Count >= this.Agreement.MaxTransmissions)
            {
                return default;
            }
            else if (transmissionId == 0 || this.sendTransmissions.TransmissionIdChain.ContainsKey(transmissionId))
            {
                return default;
            }

            var sendTransmission = new SendTransmission(this, transmissionId);
            sendTransmission.Goshujin = this.sendTransmissions;
            return sendTransmission;
        }
    }

    internal async ValueTask<SendTransmission?> TryCreateSendTransmission()
    {
Retry:
        if (this.CancellationToken.IsCancellationRequested)
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
            return sendTransmission;
        }

Wait:
        try
        {
            Task.Delay(100, this.CancellationToken).WaitAsync(TimeSpan.FromSeconds(1), this.CancellationToken)
            await Task.Delay(100, this.CancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }

        goto Retry;
    }

    internal ReceiveTransmission? TryCreateReceiveTransmission(uint transmissionId, TaskCompletionSource<NetResponse>? receivedTcs, ReceiveStream? receiveStream)
    {
        lock (this.receiveTransmissions.SyncObject)
        {
            // Release receive transmissions that have elapsed a certain time after being disposed
            var currentMics = Mics.GetSystem();//
            while (this.receiveTransmissions.DisposedListChain.TryPeek(out var transmission))
            {
                if (currentMics - transmission.DisposedMics < NetConstants.TransmissionTimeoutMics)
                {
                    break;
                }

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
                transmission.DisposedMics = Mics.GetSystem();
                this.receiveTransmissions.DisposedListChain.Enqueue(transmission);
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

    internal void ReportResend()
    {
        this.AddRtt(this.smoothedRtt * 4); // tempcode
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

            this.responseSystemMics = this.ConnectionTerminal.NetTerminal.NetSender.CurrentSystemMics;

            var owner = toBeShared.Slice(PacketHeader.Length + 2, written - 2);
            var frameType = (FrameType)BitConverter.ToUInt16(span); // FrameType
            if (frameType == FrameType.Ack)
            {// Ack
                this.ProcessReceive_Ack(endPoint, owner, currentSystemMics);
            }
            else if (frameType == FrameType.FirstGene)
            {// FirstGene
                this.ProcessReceive_FirstGene(endPoint, owner, currentSystemMics);
            }
            else if (frameType == FrameType.FollowingGene)
            {// FollowingGene
                this.ProcessReceive_FollowingGene(endPoint, owner, currentSystemMics);
            }
        }
    }

    internal void ProcessReceive_Ack(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
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

    internal void ProcessReceive_FirstGene(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
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
                var count = this.receiveTransmissions.Count - this.receiveTransmissions.DisposedListChain.Count; // Active transmissions
                if (count >= this.Agreement.MaxTransmissions)
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
            }
        }

        transmission.ProcessReceive_Gene(0, toBeShared.Slice(14)); // FirstGeneFrameCode
    }

    internal void ProcessReceive_FollowingGene(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {// Following gene
        var span = toBeShared.Span;
        if (span.Length < FirstGeneFrame.LengthExcludingFrameType)
        {
            return;
        }

        var transmissionId = BitConverter.ToUInt32(span);
        span = span.Slice(sizeof(uint));
        var genePosition = BitConverter.ToInt32(span);
        span = span.Slice(sizeof(int));
        if (genePosition == 0)
        {
            return;
        }

        ReceiveTransmission? transmission;
        lock (this.receiveTransmissions.SyncObject)
        {
            if (!this.receiveTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out transmission))
            {// No transmission
                return;
            }
        }

        transmission.ProcessReceive_Gene(genePosition, toBeShared.Slice(FirstGeneFrame.LengthExcludingFrameType));
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

        return $"{connectionString} Id:{(ushort)this.ConnectionId:x4}, EndPoint:{this.EndPoint.ToString()}";
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
            }

            this.sendTransmissions.TransmissionIdChain.Clear();
        }

        lock (this.receiveTransmissions.SyncObject)
        {
            foreach (var x in this.receiveTransmissions)
            {
                x.DisposeTransmission();
            }

            this.receiveTransmissions.TransmissionIdChain.Clear();
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
