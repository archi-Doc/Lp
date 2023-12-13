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

namespace Netsphere;

// byte[32] Key, byte[16] Iv
internal readonly record struct Embryo(ulong Salt, byte[] Key, byte[] Iv);

public abstract class Connection : IDisposable
{
    private const int LowerRttLimit = 1_000; // 1ms
    private const int UpperRttLimit = 1_000_000; // 1000ms
    // private const int InitialRtt = 200_000; // 200ms
    // private static readonly int AckDelay = (int)Mics.FromMilliseconds(10);

    public enum ConnectMode
    {
        ReuseClosed,
        OnlyConnected,
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
        this.PacketTerminal = packetTerminal;
        this.ConnectionTerminal = connectionTerminal;
        this.ConnectionId = connectionId;
        this.EndPoint = endPoint;
    }

    #region FieldAndProperty

    public NetBase NetBase { get; }

    public ConnectionTerminal ConnectionTerminal { get; }

    public PacketTerminal PacketTerminal { get; }

    public ulong ConnectionId { get; }

    public NetEndPoint EndPoint { get; }

    public ConnectionAgreementBlock Agreement { get; private set; } = ConnectionAgreementBlock.Default;

    public abstract ConnectionState State { get; }

    public bool IsOpen
        => this.State == ConnectionState.Open;

    public bool IsClosedOrDisposed
        => this.State == ConnectionState.Closed || this.State == ConnectionState.Disposed;

    public int SmoothedRtt => this.smoothedRtt;

    internal long ClosedSystemMics { get; set; }

    internal long ResponseSystemMics { get; set; }

    private readonly AsyncPulseEvent transmissionsPulse = new();

    private Embryo embryo;

    // lock (this.syncAes)
    private readonly object syncAes = new();
    private Aes? aes0;
    private Aes? aes1;

    // lock (this.transmissions.SyncObject)
    private NetTransmission.GoshujinClass transmissions = new();

    // RTT
    private int minRtt; // Minimum rtt
    private int smoothedRtt; // Smoothed rtt
    private int rttvar; // Rtt variation

    #endregion

    public void Close()
        => this.Dispose();

    internal NetTransmission? TryCreateTransmission()
    {
        lock (this.transmissions.SyncObject)
        {
            if (this.transmissions.Count >= this.Agreement.MaxTransmissions)
            {
                return default;
            }

            uint transmissionId;
            do
            {
                transmissionId = RandomVault.Pseudo.NextUInt32();
            }
            while (this.transmissions.TransmissionIdChain.ContainsKey(transmissionId));

            var transmission = new NetTransmission(this, true, transmissionId);
            transmission.Goshujin = this.transmissions;
            return transmission;
        }
    }

    internal async ValueTask<NetTransmission?> CreateTransmission()
    {
Retry:
        if (this.NetBase.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        lock (this.transmissions.SyncObject)
        {
            if (this.transmissions.Count >= this.Agreement.MaxTransmissions)
            {
                goto Wait;
            }

            uint transmissionId;
            do
            {
                transmissionId = RandomVault.Pseudo.NextUInt32();
            }
            while (this.transmissions.TransmissionIdChain.ContainsKey(transmissionId));

            var transmission = new NetTransmission(this, true, transmissionId);
            transmission.Goshujin = this.transmissions;
            return transmission;
        }

Wait:
        try
        {
            await this.transmissionsPulse.WaitAsync(TimeSpan.FromSeconds(1), this.NetBase.CancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }

        goto Retry;
    }

    internal bool RemoveTransmission(NetTransmission transmission)
    {
        lock (this.transmissions.SyncObject)
        {
            if (transmission.Goshujin == this.transmissions)
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

    internal void Initialize(ConnectionAgreementBlock agreement, Embryo embryo)
    {
        this.Agreement = agreement;
        this.embryo = embryo;
    }

    internal virtual void UpdateSendQueue(NetTransmission transmission)
    {
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
    {// { uint TransmissionId, uint GenePosition } x n
        var span = toBeShared.Span;
        NetTransmission? transmissionToDispose = null;
        lock (this.transmissions.SyncObject)
        {
            NetTransmission? previous = null;
            while (span.Length >= 8)
            {
                var transmissionId = BitConverter.ToUInt32(span);
                span = span.Slice(sizeof(uint));
                var genePosition = BitConverter.ToUInt32(span);
                span = span.Slice(sizeof(uint));

                NetTransmission? transmission;
                if (previous is not null && previous.TransmissionId == transmissionId)
                {
                    transmission = previous;
                }
                else if (!this.transmissions.TransmissionIdChain.TryGetValue(transmissionId, out transmission))
                {
                    continue;
                }

                if (transmission.ProcessReceive_Ack(genePosition))
                {// Dispose the complete transmission
                    transmissionToDispose = transmission;
                    transmissionToDispose.Goshujin = null;
                }
            }
        }

        transmissionToDispose?.DisposeInternal();
    }

    internal void ProcessReceive_FirstGene(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {
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
        var totalGenes = BitConverter.ToUInt32(span);
        span = span.Slice(sizeof(uint)); // 4

        if (rttHint > 0)
        {
            this.AddRtt(rttHint);
        }

        NetTransmission? transmission;
        lock (this.transmissions.SyncObject)
        {
            if (this.transmissions.TransmissionIdChain.TryGetValue(transmissionId, out transmission))
            {// The same TransmissionId already exists.
                return;
            }

            // New transmission
            if (this.transmissions.Count >= this.Agreement.MaxTransmissions)
            {// Maximum number reached.
                return;
            }

            if (transmissionMode == 0 && totalGenes <= this.Agreement.MaxBlockGenes)
            {
                transmission = new NetTransmission(this, false, transmissionId);
                transmission.SetState_Receiving(totalGenes);
            }
            else if (transmissionMode == 1 && totalGenes < this.Agreement.MaxStreamGenes)
            {
                transmission = new NetTransmission(this, false, transmissionId);
                transmission.SetState_ReceivingStream(totalGenes);
            }
            else
            {
                return;
            }

            transmission.Goshujin = this.transmissions;
        }

        transmission.ProcessReceive_Gene(0, toBeShared.Slice(14)); // FirstGeneFrameCode
    }

    internal void ProcessReceive_FollowingGene(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {// uint TransmissionId, uint GenePosition
        var span = toBeShared.Span;
        if (span.Length < FirstGeneFrame.LengthExcludingFrameType)
        {
            return;
        }

        var transmissionId = BitConverter.ToUInt32(span);
        span = span.Slice(sizeof(uint));
        var genePosition = BitConverter.ToUInt32(span);
        span = span.Slice(sizeof(uint));
        if (genePosition == 0)
        {
            return;
        }

        NetTransmission? transmission;
        lock (this.transmissions.SyncObject)
        {
            if (!this.transmissions.TransmissionIdChain.TryGetValue(transmissionId, out transmission))
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

        var arrayOwner = PacketPool.Rent();
        var span = arrayOwner.ByteArray.AsSpan();
        var salt = RandomVault.Pseudo.NextUInt32();

        // PacketHeaderCode
        BitConverter.TryWriteBytes(span, salt); // Salt
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)this.EndPoint.Engagement); // Engagement
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, (ushort)PacketType.EncryptedResponse); // PacketType
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

        var arrayOwner = PacketPool.Rent();
        var span = arrayOwner.ByteArray.AsSpan();
        var salt = RandomVault.Pseudo.NextUInt32();

        // PacketHeaderCode
        BitConverter.TryWriteBytes(span, salt); // Salt
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)this.EndPoint.Engagement); // Engagement
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, (ushort)PacketType.Encrypted); // PacketType
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

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ConnectionTerminal.CloseInternal(this, true);
    }

    internal void DisposeActual()
    {// lock (this.Goshujin.SyncObject)
        if (this.State == ConnectionState.Open)
        {
            this.SendCloseFrame();
        }

        this.aes0?.Dispose();
        this.aes1?.Dispose();

        // tempcode
        // this.sendTransmissions.Dispose();
    }

    public override string ToString()
    {
        var connectionString = "Connection";
        if (this is ServerConnection)
        {
            connectionString = "ServerConnection";
        }
        else if (this is ClientConnection)
        {
            connectionString = "ClientConnection";
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
