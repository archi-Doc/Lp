// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Arc.Collections;
using Netsphere.Crypto;
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
    private const int LowerRttLimit = 5_000; // 5ms
    private const int UpperRttLimit = 1_000_000; // 1000ms
    private const int DefaultRtt = 100_000; // 100ms

    public enum ConnectMode
    {
        ReuseIfAvailable,
        ReuseOnly,
        NoReuse,
    }

    public enum State
    {
        Open,
        Closed,
        Disposed,
    }

    public Connection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetNode node, NetEndPoint endPoint)
    {
        this.NetBase = connectionTerminal.NetBase;
        this.Logger = this.NetBase.UnitLogger.GetLogger(this.GetType());
        this.PacketTerminal = packetTerminal;
        this.ConnectionTerminal = connectionTerminal;
        this.ConnectionId = connectionId;
        this.DestinationNode = node;
        this.DestinationEndPoint = endPoint;

        this.smoothedRtt = DefaultRtt;
        this.minimumRtt = 0;
        this.UpdateLastEventMics();
    }

    public Connection(Connection connection)
        : this(connection.PacketTerminal, connection.ConnectionTerminal, connection.ConnectionId, connection.DestinationNode, connection.DestinationEndPoint)
    {
        this.Initialize(connection.Agreement, connection.embryo);
    }

    #region FieldAndProperty

    public NetBase NetBase { get; }

    public NetTerminal NetTerminal => this.ConnectionTerminal.NetTerminal;

    internal ConnectionTerminal ConnectionTerminal { get; }

    internal PacketTerminal PacketTerminal { get; }

    public ulong ConnectionId { get; }

    public string ConnectionIdText
        => ((ushort)this.ConnectionId).ToString("x4");

    public NetNode DestinationNode { get; }

    public NetEndPoint DestinationEndPoint { get; }

    public ulong Salt
        => this.embryo.Salt;

    public ConnectionAgreement Agreement { get; private set; } = ConnectionAgreement.Default;

    public State CurrentState { get; private set; }

    public abstract bool IsClient { get; }

    public abstract bool IsServer { get; }

    public bool IsActive
        => this.NetTerminal.IsActive && this.CurrentState == State.Open;

    public bool IsOpen
        => this.CurrentState == State.Open;

    public bool IsClosed
        => this.CurrentState == State.Closed;

    public bool IsDisposed
        => this.CurrentState == State.Disposed;

    public bool IsClosedOrDisposed
        => this.CurrentState == State.Closed || this.CurrentState == State.Disposed;

    public int SmoothedRtt
        => this.smoothedRtt;

    public int MinimumRtt
        => this.minimumRtt == 0 ? this.smoothedRtt : this.minimumRtt;

    public int LatestRtt
        => this.latestRtt;

    public int RttVar
        => this.rttvar;

    // this.smoothedRtt + Math.Max(this.rttvar * 4, 1_000) + NetConstants.AckDelayMics; // 10ms
    public int RetransmissionTimeout
        => this.smoothedRtt + (this.smoothedRtt >> 2) + (this.rttvar << 2) + NetConstants.AckDelayMics;

    public int TaichiTimeout
        => this.RetransmissionTimeout * this.Taichi;

    public int SendCount
        => this.sendCount;

    public int ResendCount
        => this.resendCount;

    public double DeliveryRatio
    {
        get
        {
            var total = this.sendCount + this.resendCount;
            return total == 0 ? 1.0d : (this.sendCount / (double)total);
        }
    }

    public long ConnectionRetentionMics { get; set; }

    internal ILogger Logger { get; }

    internal int SendTransmissionsCount
        => this.sendTransmissions.Count;

    internal bool IsEmpty
        => this.sendTransmissions.Count == 0 &&
        this.receiveTransmissions.Count == 0;

    internal bool CloseIfTransmissionHasTimedOut()
    {
        if (this.LastEventMics + NetConstants.TransmissionTimeoutMics < Mics.FastSystem)
        {// Timeout
            this.ConnectionTerminal.CloseInternal(this, true);
            return true;
        }
        else
        {
            return false;
        }
    }

    internal long LastEventMics { get; private set; } // When any packet, including an Ack, is received, it's updated to the latest time.

    internal ICongestionControl? CongestionControl; // ConnectionTerminal.SyncSend
    internal UnorderedLinkedList<SendTransmission> SendList = new(); // lock (this.ConnectionTerminal.SyncSend)
    internal UnorderedLinkedList<Connection>.Node? SendNode; // lock (this.ConnectionTerminal.SyncSend)

    private Embryo embryo;

    // lock (this.syncAes)
    private readonly object syncAes = new();
    private Aes? aes0;
    private Aes? aes1;

    private SendTransmission.GoshujinClass sendTransmissions = new(); // lock (this.sendTransmissions.SyncObject)
    private UnorderedLinkedList<SendTransmission> sendAckedList = new();

    // ReceiveTransmissionCode, lock (this.receiveTransmissions.SyncObject)
    private ReceiveTransmission.GoshujinClass receiveTransmissions = new();
    private UnorderedLinkedList<ReceiveTransmission> receiveReceivedList = new();
    private UnorderedLinkedList<ReceiveTransmission> receiveDisposedList = new();

    // RTT
    private int minimumRtt; // Minimum rtt (mics)
    private int smoothedRtt; // Smoothed rtt (mics)
    private int latestRtt; // Latest rtt (mics)
    private int rttvar; // Rtt variation (mics)
    private int sendCount;
    private int resendCount;

    // Ack
    internal long AckMics; // lock(AckBuffer.syncObject)
    internal Queue<AckBuffer.ReceiveTransmissionAndAckGene>? AckQueue; // lock(AckBuffer.syncObject)

    // Connection lost
    internal int Taichi = 1;

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateAckedNode(SendTransmission sendTransmission)
    {// lock (Connection.sendTransmissions.SyncObject)
        sendTransmission.AckedMics = Mics.FastSystem;
        this.sendTransmissions.AckedListChain.AddLast(sendTransmission);

        /*if (sendTransmission.AckedNode is null)
        {
            sendTransmission.AckedNode = this.sendAckedList.AddLast(sendTransmission);
        }
        else
        {
            this.sendAckedList.MoveToLast(sendTransmission.AckedNode);
        }*/
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ChangeStateInternal(State state)
    {// lock (this.clientConnections.SyncObject) or lock (this.serverConnections.SyncObject)
        if (this.CurrentState == state)
        {
            if (this.CurrentState == State.Open)
            {
                this.UpdateLastEventMics();
            }

            return;
        }

        this.CurrentState = state;
        this.UpdateLastEventMics();
        this.OnStateChanged();
    }

    public void ApplyAgreement()
    {
        this.ConnectionRetentionMics = (long)this.Agreement.MinimumConnectionRetentionSeconds * 1_000_000;
    }

    public bool SignWithSalt<T>(T value, SignaturePrivateKey privateKey)
        where T : ITinyhandSerialize<T>, ISignAndVerify
    {
        value.Salt = this.Salt;
        return value.Sign(privateKey);
    }

    public bool ValidateAndVerifyWithSalt<T>(T value)
        where T : ITinyhandSerialize<T>, ISignAndVerify
    {
        if (value.Salt != this.Salt)
        {
            return false;
        }

        return NetHelper.ValidateAndVerify(value);
    }

    public void Close()
        => this.Dispose();

    internal void ResetTaichi()
        => this.Taichi = 1;

    internal void DoubleTaichi()
    {
        this.Taichi <<= 1;
        if (this.Taichi < 1)
        {
            this.Taichi = 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateLastEventMics()
        => this.LastEventMics = Mics.FastSystem;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ICongestionControl GetCongestionControl()
    {
        return this.CongestionControl is null ? this.ConnectionTerminal.NoCongestionControl : this.CongestionControl;
    }

    internal void CreateCongestionControl()
    {
        while (true)
        {
            if (this.CongestionControl is not null)
            {
                return;
            }

            var congestionControl = new CubicCongestionControl(this);
            if (Interlocked.CompareExchange(ref this.CongestionControl, congestionControl, null) == null)
            {
                lock (this.ConnectionTerminal.CongestionControlList)
                {
                    this.ConnectionTerminal.CongestionControlList.AddLast(congestionControl);
                }

                return;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddSend(SendTransmission transmission)
    {
        var list = this.ConnectionTerminal.SendList;
        lock (this.ConnectionTerminal.SyncSend)
        {
            if (this.SendNode is null)
            {
                this.SendNode = list.AddLast(this);
            }

            if (transmission.SendNode is null)
            {
                transmission.SendNode = this.SendList.AddLast(transmission);
            }
        }
    }

    internal SendTransmission? TryCreateSendTransmission()
    {
        if (!this.IsActive)
        {
            return default;
        }

        lock (this.sendTransmissions.SyncObject)
        {
            if (this.IsClosedOrDisposed ||
                this.SendTransmissionsCount >= this.Agreement.MaxTransmissions)
            {//
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
    }

    internal SendTransmission? TryCreateSendTransmission(uint transmissionId)
    {
        lock (this.sendTransmissions.SyncObject)
        {
            if (this.IsClosedOrDisposed)
            {
                return default;
            }

            /* To maintain consistency with the number of SendTransmission on the client side, limit the number of ReceiveTransmission in ProcessReceive_FirstGene().
            if (this.NumberOfSendTransmissions >= this.Agreement.MaxTransmissions)
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
        if (!this.IsActive || timeout < TimeSpan.Zero)
        {
            return default;
        }

        lock (this.sendTransmissions.SyncObject)
        {
            if (this.IsClosedOrDisposed)
            {
                return default;
            }

            if (this.SendTransmissionsCount >= this.Agreement.MaxTransmissions)
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
            await Task.Delay(NetConstants.CreateTransmissionDelay).ConfigureAwait(false);
        }
        catch
        {// Cancelled
            return default;
        }

        timeout -= NetConstants.CreateTransmissionDelay;
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void CleanTransmission()
    {
        if (this.sendTransmissions.Count > 0)
        {
            lock (this.sendTransmissions.SyncObject)
            {
                this.CleanSendTransmission();
            }
        }
    }

    internal void CleanSendTransmission()
    {// lock (this.sendTransmissions.SyncObject)
        // Release send transmissions that have elapsed a certain time since the last ack.
        var currentMics = Mics.FastSystem;
        while (this.sendAckedList.First is { } node)
        {
            var transmission = node.Value;
            if (currentMics < transmission.AckedMics + NetConstants.TransmissionTimeoutMics)
            {
                break;
            }

            transmission.DisposeTransmission();
            // node.List.Remove(node);
            // transmission.AckedNode = null;
            transmission.Goshujin = null;
        }
    }

    internal void CleanReceiveTransmission()
    {// lock (this.receiveTransmissions.SyncObject)
        Debug.Assert(this.receiveTransmissions.Count == (this.receiveReceivedList.Count + this.receiveDisposedList.Count));

        // Release receive transmissions that have elapsed a certain time after being disposed.
        var currentMics = Mics.FastSystem;
        while (this.receiveDisposedList.First is { } node)
        {
            var transmission = node.Value;
            if (currentMics < transmission.ReceivedOrDisposedMics + NetConstants.TransmissionDisposalMics)
            {
                break;
            }

            node.List.Remove(node);
            transmission.ReceivedOrDisposedNode = null;
            transmission.Goshujin = null;
        }

        // Release receive transmissions that have elapsed a certain time since the last data reception.
        while (this.receiveReceivedList.First is { } node)
        {
            var transmission = node.Value;
            if (currentMics < transmission.ReceivedOrDisposedMics + NetConstants.TransmissionTimeoutMics)
            {
                break;
            }

            node.List.Remove(node);
            transmission.DisposeTransmission();
            transmission.ReceivedOrDisposedNode = null;
            transmission.Goshujin = null;
        }
    }

    internal ReceiveTransmission? TryCreateReceiveTransmission(uint transmissionId, TaskCompletionSource<NetResponse>? receivedTcs)
    {
        transmissionId += this.ConnectionTerminal.ReceiveTransmissionGap;

        lock (this.receiveTransmissions.SyncObject)
        {
            this.CleanReceiveTransmission();

            if (this.IsClosedOrDisposed)
            {
                return default;
            }

            if (this.receiveTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out var receiveTransmission))
            {
                return default;
            }

            receiveTransmission = new ReceiveTransmission(this, transmissionId, receivedTcs);
            receiveTransmission.ReceivedOrDisposedMics = Mics.FastSystem;
            receiveTransmission.ReceivedOrDisposedNode = this.receiveReceivedList.AddLast(receiveTransmission);
            receiveTransmission.Goshujin = this.receiveTransmissions;
            return receiveTransmission;
        }
    }

    /*internal ReceiveTransmission? TryCreateOrReuseReceiveTransmission(uint transmissionId, TaskCompletionSource<NetResponse>? receivedTcs)
    {
        transmissionId += this.ConnectionTerminal.ReceiveTransmissionGap;

        lock (this.receiveTransmissions.SyncObject)
        {
            // this.CleanReceiveTransmission();

            if (this.IsClosedOrDisposed)
            {
                return default;
            }

            if (this.receiveTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out var receiveTransmission))
            {
                if (receiveTransmission.Mode == NetTransmissionMode.Initial)
                {
                    receiveTransmission.Reset(receivedTcs);
                    return receiveTransmission;
                }
                else if (receiveTransmission.Mode == NetTransmissionMode.Disposed)
                {
                    receiveTransmission.ReceivedOrDisposedMics = Mics.FastSystem;
                    if (receiveTransmission.ReceivedOrDisposedNode is { } node)
                    {
                        node.List.Remove(node);
                    }

                    receiveTransmission.ReceivedOrDisposedNode = this.receiveReceivedList.AddLast(receiveTransmission);
                    receiveTransmission.Reset(receivedTcs);
                    return receiveTransmission;
                }
                else
                {
                    return default;
                }
            }

            receiveTransmission = new ReceiveTransmission(this, transmissionId, receivedTcs);
            receiveTransmission.ReceivedOrDisposedMics = Mics.FastSystem;
            receiveTransmission.ReceivedOrDisposedNode = this.receiveReceivedList.AddLast(receiveTransmission);
            receiveTransmission.Goshujin = this.receiveTransmissions;
            return receiveTransmission;
        }
    }*/

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
                transmission.ReceivedOrDisposedMics = Mics.FastSystem; // Disposed mics
                if (transmission.ReceivedOrDisposedNode is { } node)
                {// ReceivedList -> DisposedList
                    node.List.Remove(node);
                    transmission.ReceivedOrDisposedNode = this.receiveDisposedList.AddLast(transmission);
                    Debug.Assert(transmission.ReceivedOrDisposedNode.List != null);
                }
                else
                {// -> DisposedList
                    transmission.ReceivedOrDisposedNode = this.receiveDisposedList.AddLast(transmission);
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

    internal void Initialize(ConnectionAgreement agreement, Embryo embryo)
    {
        this.Agreement = agreement;
        this.embryo = embryo;
        this.ApplyAgreement();
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

        this.latestRtt = rttMics;
        if (this.minimumRtt == 0)
        {// Initial
            this.minimumRtt = rttMics;
            this.smoothedRtt = rttMics;
            this.rttvar = rttMics >> 1;
        }
        else
        {// Update
            if (this.minimumRtt > rttMics)
            {// minRtt is greater then the latest rtt.
                this.minimumRtt = rttMics;
            }

            var adjustedRtt = rttMics; // - ackDelay
            this.smoothedRtt = ((this.smoothedRtt * 7) >> 3) + (adjustedRtt >> 3);
            var rttvarSample = Math.Abs(this.smoothedRtt - adjustedRtt);
            this.rttvar = ((this.rttvar * 3) >> 2) + (rttvarSample >> 2);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementResendCount()
    {
        this.resendCount++; // Not thread-safe, though it doesn't matter.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementSendCount()
    {
        this.sendCount++; // Not thread-safe, though it doesn't matter.
    }

    internal void SendPriorityFrame(scoped Span<byte> frame)
    {// Close, Ack, Knock
        if (!this.CreatePacket(frame, out var owner))
        {
            return;
        }

        this.PacketTerminal.AddSendPacket(this.DestinationEndPoint.EndPoint, owner, default);
    }

    internal void SendCloseFrame()
    {
        // this.SendPriorityFrame([]); // Close 1 (Obsolete)

        Span<byte> frame = stackalloc byte[2]; // Close 2
        var span = frame;
        BitConverter.TryWriteBytes(span, (ushort)FrameType.Close);
        span = span.Slice(sizeof(ushort));
        this.SendPriorityFrame(frame);
    }

    internal ProcessSendResult ProcessSingleSend(NetSender netSender)
    {// lock (this.ConnectionTerminal.SyncSend)
        if (this.IsClosedOrDisposed)
        {// Connection closed
            return ProcessSendResult.Complete;
        }

        var node = this.SendList.First;
        if (node is null)
        {// No transmission to send
            return ProcessSendResult.Complete;
        }

        var transmission = node.Value;
        Debug.Assert(transmission.SendNode == node);

        if (this.LastEventMics + NetConstants.TransmissionTimeoutMics < Mics.FastSystem)
        {// Timeout
            this.ConnectionTerminal.CloseInternal(this, true);
            return ProcessSendResult.Complete;
        }

        var congestionControl = this.GetCongestionControl();
        if (congestionControl.IsCongested)
        {// If in a congested state, return ProcessSendResult.Congestion.
            return ProcessSendResult.Congested;
        }

        var result = transmission.ProcessSingleSend(netSender);
        if (result == ProcessSendResult.Complete)
        {// Delete the node if there is no gene to send.
            this.SendList.Remove(node);
            transmission.SendNode = default;
        }
        else if (result == ProcessSendResult.Remaining)
        {// If there are remaining genes, move it to the end.
            this.SendList.MoveToLast(node);
        }

        return this.SendList.Count == 0 ? ProcessSendResult.Complete : ProcessSendResult.Remaining;
    }

    internal void ProcessReceive(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {// endPoint: Checked
        if (this.CurrentState == State.Disposed)
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
        {// Close 1 (Obsolete)
            // this.ConnectionTerminal.CloseInternal(this, false);
            return;
        }

        if (packetType == PacketType.Protected || packetType == PacketType.ProtectedResponse)
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
            if (frameType == FrameType.Close)
            {// Close 2
                this.ConnectionTerminal.CloseInternal(this, false);
            }
            else if (frameType == FrameType.Ack)
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
            else if (frameType == FrameType.Knock)
            {// Knock
                this.ProcessReceive_Knock(endPoint, owner);
            }
            else if (frameType == FrameType.KnockResponse)
            {// KnockResponse
                this.ProcessReceive_KnockResponse(endPoint, owner);
            }
        }
    }

    internal void ProcessReceive_Ack(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
    {// uint TransmissionId, ushort NumberOfPairs, { int StartGene, int EndGene } x pairs
        var span = toBeShared.Span;
        lock (this.sendTransmissions.SyncObject)
        {
            while (span.Length >= 8)
            {
                var maxReceivePosition = BitConverter.ToInt32(span);
                span = span.Slice(sizeof(int));
                var transmissionId = BitConverter.ToUInt32(span);
                span = span.Slice(sizeof(uint));

                if (maxReceivePosition < 0)
                {// Rama (Complete)
                    if (this.sendTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out var transmission))
                    {
                        this.UpdateAckedNode(transmission);
                        this.UpdateLastEventMics();
                        transmission.ProcessReceive_AckRama();
                    }
                    else
                    {// SendTransmission has already been disposed due to reasons such as having already received response data.
                    }
                }
                else
                {// Block/Stream
                    if (span.Length < 6)
                    {
                        break;
                    }

                    var successiveReceivedPosition = BitConverter.ToInt32(span);
                    span = span.Slice(sizeof(int));
                    var numberOfPairs = BitConverter.ToUInt16(span);
                    span = span.Slice(sizeof(ushort));

                    var length = numberOfPairs * 8;
                    if (span.Length < length)
                    {
                        break;
                    }

                    if (this.sendTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out var transmission))
                    {
                        this.UpdateAckedNode(transmission);
                        this.UpdateLastEventMics();
                        transmission.ProcessReceive_AckBlock(maxReceivePosition, successiveReceivedPosition, span, numberOfPairs);
                    }
                    else
                    {// SendTransmission has already been disposed due to reasons such as having already received response data.
                    }

                    span = span.Slice(length);
                }
            }

            Debug.Assert(span.Length == 0);
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
        var dataControl = (DataControl)BitConverter.ToUInt16(span);
        span = span.Slice(sizeof(ushort)); // 2
        var rttHint = BitConverter.ToInt32(span);
        span = span.Slice(sizeof(int)); // 4
        var totalGenes = BitConverter.ToInt32(span);

        if (rttHint > 0)
        {
            this.AddRtt(rttHint);
        }

        ReceiveTransmission? transmission;
        long maxStreamLength = 0;
        ulong dataId = 0;
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
                    this.ConnectionTerminal.AckQueue.AckBlock(this, transmission, 0); // Resend the ACK in case it was not received.
                    return;
                }

                if (transmissionMode == 0 && totalGenes <= this.Agreement.MaxBlockGenes)
                {// Block mode
                    transmission.SetState_Receiving(totalGenes);
                }
                else if (transmissionMode == 1)
                {// Stream mode
                    maxStreamLength = BitConverter.ToInt64(span);
                    span = span.Slice(sizeof(int) + sizeof(uint)); // 8
                    dataId = BitConverter.ToUInt64(span);

                    if (!this.Agreement.CheckStreamLength(maxStreamLength))
                    {
                        return;
                    }

                    transmission.SetState_ReceivingStream(maxStreamLength);
                }
                else
                {
                    return;
                }
            }
            else
            {// Server side
                if (this.receiveTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out transmission))
                {// The same TransmissionId already exists.
                    this.ConnectionTerminal.AckQueue.AckBlock(this, transmission, 0); // Resend the ACK in case it was not received.
                    return;
                }

                this.CleanReceiveTransmission();

                // New transmission
                if (this.receiveReceivedList.Count >= this.Agreement.MaxTransmissions)
                {// Maximum number reached.
                    return;
                }

                if (transmissionMode == 0 && totalGenes <= this.Agreement.MaxBlockGenes)
                {// Block mode
                    transmission = new(this, transmissionId, default);
                    transmission.SetState_Receiving(totalGenes);
                }
                else if (transmissionMode == 1)
                {// Stream mode
                    maxStreamLength = BitConverter.ToInt64(span);
                    span = span.Slice(sizeof(int) + sizeof(uint)); // 8
                    dataId = BitConverter.ToUInt64(span);

                    if (!this.Agreement.CheckStreamLength(maxStreamLength))
                    {
                        return;
                    }

                    transmission = new(this, transmissionId, default);
                    transmission.SetState_ReceivingStream(maxStreamLength);
                }
                else
                {
                    return;
                }

                transmission.Goshujin = this.receiveTransmissions;
                transmission.ReceivedOrDisposedMics = Mics.FastSystem; // Received mics
                transmission.ReceivedOrDisposedNode = this.receiveReceivedList.AddLast(transmission);
            }
        }

        this.UpdateLastEventMics();

        // FirstGeneFrameCode (DataKind + DataId + Data...)
        transmission.ProcessReceive_Gene(dataControl, 0, toBeShared.Slice(16));

        if (transmission.Mode == NetTransmissionMode.Stream)
        {// Invoke stream
            if (this is ServerConnection serverConnection)
            {
                serverConnection.GetContext().InvokeStream(transmission, dataId, maxStreamLength);
            }
            else if (this is ClientConnection clientConnection)
            {
                transmission.StartStream(dataId, maxStreamLength);
            }
        }
    }

    internal void ProcessReceive_FollowingGene(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
    {// Following gene
        var span = toBeShared.Span;
        if (span.Length < FollowingGeneFrame.LengthExcludingFrameType)
        {
            return;
        }

        // FollowingGeneFrameCode
        var transmissionId = BitConverter.ToUInt32(span);
        span = span.Slice(sizeof(uint));
        var dataControl = (DataControl)BitConverter.ToUInt16(span);
        span = span.Slice(sizeof(ushort)); // 2

        var dataPosition = BitConverter.ToInt32(span);
        if (dataPosition == 0)
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

            // ReceiveTransmissionsCode
            if (transmission.ReceivedOrDisposedNode is { } node &&
                node.List == this.receiveReceivedList)
            {
                transmission.ReceivedOrDisposedMics = Mics.FastSystem; // Received mics
                this.receiveReceivedList.MoveToLast(node);
            }
        }

        this.UpdateLastEventMics();
        transmission.ProcessReceive_Gene(dataControl, dataPosition, toBeShared.Slice(FollowingGeneFrame.LengthExcludingFrameType));
    }

    internal void ProcessReceive_Knock(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
    {// KnockResponseFrameCode
        if (toBeShared.Memory.Length < (KnockFrame.Length - 2))
        {
            return;
        }

        ReceiveTransmission? transmission;
        var transmissionId = BitConverter.ToUInt32(toBeShared.Span);
        lock (this.receiveTransmissions.SyncObject)
        {
            if (!this.receiveTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out transmission))
            {
                return;
            }
        }

        Span<byte> frame = stackalloc byte[KnockResponseFrame.Length];
        var span = frame;
        BitConverter.TryWriteBytes(span, (ushort)FrameType.KnockResponse);
        span = span.Slice(sizeof(ushort));
        BitConverter.TryWriteBytes(span, transmission.TransmissionId);
        span = span.Slice(sizeof(uint));
        BitConverter.TryWriteBytes(span, transmission.MaxReceivePosition);
        span = span.Slice(sizeof(int));

        this.SendPriorityFrame(frame);
    }

    internal void ProcessReceive_KnockResponse(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
    {// KnockResponseFrameCode
        var span = toBeShared.Span;
        if (span.Length < (KnockResponseFrame.Length - 2))
        {
            return;
        }

        var transmissionId = BitConverter.ToUInt32(span);
        span = span.Slice(sizeof(uint));
        lock (this.sendTransmissions.SyncObject)
        {
            if (this.sendTransmissions.TransmissionIdChain.TryGetValue(transmissionId, out var transmission))
            {
                var maxReceivePosition = BitConverter.ToInt32(span);
                span = span.Slice(sizeof(int));

                transmission.MaxReceivePosition = maxReceivePosition;

                this.Logger.TryGet(LogLevel.Debug)?.Log($"KnockResponse: {maxReceivePosition}");
            }
        }
    }

    internal bool CreatePacket(scoped ReadOnlySpan<byte> frame, out ByteArrayPool.MemoryOwner owner)
    {
        if (frame.Length > PacketHeader.MaxFrameLength)
        {
            owner = default;
            return false;
        }

        var packetType = this is ClientConnection ? PacketType.Protected : PacketType.ProtectedResponse;
        var arrayOwner = PacketPool.Rent();
        var span = arrayOwner.ByteArray.AsSpan();
        var salt = RandomVault.Pseudo.NextUInt32();

        // PacketHeaderCode
        BitConverter.TryWriteBytes(span, salt); // Salt
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)this.DestinationEndPoint.Engagement); // Engagement
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

    internal void CreatePacket(scoped Span<byte> frameHeader, scoped ReadOnlySpan<byte> frameContent, out ByteArrayPool.MemoryOwner owner)
    {
        Debug.Assert((frameHeader.Length + frameContent.Length) <= PacketHeader.MaxFrameLength);

        var packetType = this is ClientConnection ? PacketType.Protected : PacketType.ProtectedResponse;
        var arrayOwner = PacketPool.Rent();
        var span = arrayOwner.ByteArray.AsSpan();
        var salt = RandomVault.Pseudo.NextUInt32();

        // PacketHeaderCode
        BitConverter.TryWriteBytes(span, salt); // Salt
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)this.DestinationEndPoint.Engagement); // Engagement
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

    internal void CreateAckPacket(ByteArrayPool.Owner owner, int length, out int packetLength)
    {
        var packetType = this is ClientConnection ? PacketType.Protected : PacketType.ProtectedResponse;
        var span = owner.ByteArray.AsSpan();
        var salt = RandomVault.Pseudo.NextUInt32();

        // PacketHeaderCode
        BitConverter.TryWriteBytes(span, salt); // Salt
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)this.DestinationEndPoint.Engagement); // Engagement
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, (ushort)packetType); // PacketType
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.ConnectionId); // Id
        span = span.Slice(sizeof(ulong));

        var source = span;
        BitConverter.TryWriteBytes(span, (ushort)FrameType.Ack); // Frame type
        span = span.Slice(sizeof(ushort));

        this.TryEncryptCbc(salt, source.Slice(0, sizeof(ushort) + length), PacketPool.MaxPacketSize - PacketHeader.Length, out var written);
        packetLength = PacketHeader.Length + written;
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {// Close the connection, but defer actual disposal for reuse.
        this.ConnectionTerminal.CloseInternal(this, true);
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

        return $"{connectionString} Id:{(ushort)this.ConnectionId:x4}, EndPoint:{this.DestinationEndPoint.ToString()}, Delivery:{this.DeliveryRatio.ToString("F2")} ({this.SendCount}/{this.SendCount + this.ResendCount})";
    }

    protected void ReleaseResource()
    {
        lock (this.syncAes)
        {
            if (this.aes0 is not null)
            {
                this.aes0.Dispose();
                this.aes0 = default;
            }

            if (this.aes1 is not null)
            {
                this.aes1.Dispose();
                this.aes1 = default;
            }
        }
    }

    internal bool TryEncryptCbc(uint salt, ReadOnlySpan<byte> source, Span<byte> destination, out int written)
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

    internal void CloseAllTransmission()
    {
        lock (this.sendTransmissions.SyncObject)
        {
            foreach (var x in this.sendTransmissions)
            {
                if (x.IsDisposed)
                {
                    continue;
                }

                x.DisposeTransmission();
                // x.Goshujin = null;
            }

            // Since it's within a lock statement, manually clear it.
            this.sendTransmissions.TransmissionIdChain.Clear();
        }

        lock (this.receiveTransmissions.SyncObject)
        {
            foreach (var x in this.receiveTransmissions)
            {
                x.DisposeTransmission();

                if (x.ReceivedOrDisposedNode is { } node)
                {
                    node.List.Remove(node);
                    x.ReceivedOrDisposedNode = null;
                }

                // x.Goshujin = null;
            }

            // Since it's within a lock statement, manually clear it.
            // ReceiveTransmissionsCode
            this.receiveTransmissions.TransmissionIdChain.Clear();
            this.receiveReceivedList.Clear();
            this.receiveDisposedList.Clear();
        }
    }

    internal void CloseSendTransmission()
    {
        lock (this.sendTransmissions.SyncObject)
        {
            foreach (var x in this.sendTransmissions)
            {
                if (x.IsDisposed)
                {
                    continue;
                }

                x.DisposeTransmission();
                // x.Goshujin = null;
            }

            // Since it's within a lock statement, manually clear it.
            this.sendTransmissions.TransmissionIdChain.Clear();
        }
    }

    internal virtual void OnStateChanged()
    {
        if (this.CurrentState == State.Disposed)
        {
            this.ReleaseResource();
        }
    }

    internal void TerminateInternal()
    {
        Queue<SendTransmission>? sendQueue = default;
        lock (this.sendTransmissions.SyncObject)
        {
            foreach (var x in this.sendTransmissions)
            {
                if (x.Mode == NetTransmissionMode.Stream ||
                    x.Mode == NetTransmissionMode.StreamCompleted)
                {// Terminate stream transmission.
                    x.DisposeTransmission();
                }

                if (x.IsDisposed)
                {
                    sendQueue ??= new();
                    sendQueue.Enqueue(x);
                }
            }

            if (sendQueue is not null)
            {
                while (sendQueue.TryDequeue(out var t))
                {
                    t.Goshujin = default;
                }
            }
        }

        Queue<ReceiveTransmission>? receiveQueue = default;
        lock (this.receiveTransmissions.SyncObject)
        {
            foreach (var x in this.receiveTransmissions)
            {
                if (x.Mode == NetTransmissionMode.Stream ||
                    x.Mode == NetTransmissionMode.StreamCompleted)
                {// Terminate stream transmission.
                    x.DisposeTransmission();
                }

                if (x.IsDisposed)
                {
                    receiveQueue ??= new();
                    receiveQueue.Enqueue(x);
                }
            }

            if (receiveQueue is not null)
            {
                while (receiveQueue.TryDequeue(out var t))
                {
                    t.Goshujin = default;
                }
            }
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
