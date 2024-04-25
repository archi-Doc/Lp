// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using Netsphere.Core;
using Netsphere.Relay;
using Netsphere.Stats;
using Tinyhand.IO;

namespace Netsphere.Packet;

public sealed partial class PacketTerminal
{
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private sealed partial class Item
    {
        // ResponseTcs == null: WaitingToSend -> (Send) -> (Remove)
        // ResponseTcs != null: WaitingToSend -> WaitingForResponse -> Complete or Resend
        [Link(Type = ChainType.LinkedList, Name = "WaitingToSendList", AutoLink = true)]
        [Link(Type = ChainType.LinkedList, Name = "WaitingForResponseList", AutoLink = false)]
        public Item(IPEndPoint endPoint, ByteArrayPool.MemoryOwner dataToBeMoved, TaskCompletionSource<NetResponse>? responseTcs)
        {
            if (dataToBeMoved.Span.Length < PacketHeader.Length)
            {
                throw new InvalidOperationException();
            }

            this.EndPoint = endPoint;
            this.PacketId = BitConverter.ToUInt64(dataToBeMoved.Span.Slice(10)); // PacketHeaderCode
            this.MemoryOwner = dataToBeMoved;
            this.ResponseTcs = responseTcs;
        }

        [Link(Primary = true, Type = ChainType.Unordered)]
        public ulong PacketId { get; set; }

        public IPEndPoint EndPoint { get; }

        public ByteArrayPool.MemoryOwner MemoryOwner { get; }

        public TaskCompletionSource<NetResponse>? ResponseTcs { get; }

        public long SentMics { get; set; }

        public int ResentCount { get; set; }

        public void Remove()
        {
            this.MemoryOwner.Return();
            this.Goshujin = null;
        }
    }

    public PacketTerminal(NetBase netBase, NetStats netStats, NetTerminal netTerminal, ILogger<PacketTerminal> logger)
    {
        this.netBase = netBase;
        this.netStats = netStats;
        this.netTerminal = netTerminal;
        this.logger = logger;

        this.RetransmissionTimeoutMics = NetConstants.DefaultRetransmissionTimeoutMics;
        this.MaxResendCount = 2;
    }

    public long RetransmissionTimeoutMics { get; set; }

    public int MaxResendCount { get; set; }

    private readonly NetBase netBase;
    private readonly NetStats netStats;
    private readonly NetTerminal netTerminal;
    private readonly ILogger logger;
    private readonly Item.GoshujinClass items = new();

    /// <summary>
    /// Sends a packet to a specified address and waits for a response.
    /// </summary>
    /// <typeparam name="TSend">The type of the packet to send. Must implement IPacket and ITinyhandSerialize.</typeparam>
    /// <typeparam name="TReceive">The type of the packet to receive. Must implement IPacket and ITinyhandSerialize.</typeparam>
    /// <param name="netAddress">The address to send the packet to.</param>
    /// <param name="packet">The packet to send.</param>
    /// <param name="relayNumber">Specify the minimum number of relays or the target relay [default is 0].<br/>
    /// relayNumber &lt; 0: The target relay.<br/>
    /// relayNumber == 0: Relays are not necessary.<br/>
    /// relayNumber &gt; 0: The minimum number of relays.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task<(NetResult Result, TReceive? Value, int RttMics)> SendAndReceive<TSend, TReceive>(NetAddress netAddress, TSend packet, int relayNumber = 0)
        where TSend : IPacket, ITinyhandSerialize<TSend>
        where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        if (!this.netTerminal.IsActive)
        {
            return (NetResult.Closed, default, 0);
        }

        if (!this.netTerminal.TryCreateEndpoint(in netAddress, out var endpoint))
        {
            return (NetResult.NoNetwork, default, 0);
        }

        var responseTcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreatePacket(0, packet, out var owner); // CreatePacketCode
        var result = this.SendPacket(netAddress, endpoint, owner, responseTcs, relayNumber);
        if (result != NetResult.Success)
        {
            return (result, default, 0);
        }

        if (NetConstants.LogLowLevelNet)
        {
            // this.logger.TryGet(LogLevel.Debug)?.Log($"{this.netTerminal.NetTerminalString} to {endPoint.ToString()} {owner.Span.Length} {typeof(TSend).Name}/{typeof(TReceive).Name}");
        }

        try
        {
            var response = await this.netTerminal.Wait(responseTcs.Task, this.netTerminal.PacketTransmissionTimeout, default).ConfigureAwait(false);

            if (response.IsFailure)
            {
                return new(response.Result, default, 0);
            }

            TReceive? receive;
            try
            {
                receive = TinyhandSerializer.DeserializeObject<TReceive>(response.Received.Span.Slice(PacketHeader.Length));
            }
            catch
            {
                return new(NetResult.DeserializationFailed, default, 0);
            }
            finally
            {
                response.Return();
            }

            return (NetResult.Success, receive, (int)response.Additional);
        }
        catch
        {
            return (NetResult.Timeout, default, 0);
        }
    }

    /*/// <summary>
    /// Sends a packet to a specified address and waits for a response.
    /// </summary>
    /// <typeparam name="TSend">The type of the packet to send. Must implement IPacket and ITinyhandSerialize.</typeparam>
    /// <typeparam name="TReceive">The type of the packet to receive. Must implement IPacket and ITinyhandSerialize.</typeparam>
    /// <param name="endPoint">The endpoint to send the packet to.</param>
    /// <param name="packet">The packet to send.</param>
    /// <param name="relayNumber">Specify the minimum number of relays or the target relay [default is 0].<br/>
    /// &lt; 0: The target relay.<br/>
    /// 0: Relays are not necessary.<br/>
    /// 0 &gt;: The minimum number of relays.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task<(NetResult Result, TReceive? Value, int RttMics)> SendAndReceive<TSend, TReceive>(NetEndpoint endPoint, TSend packet, int relayNumber = 0)
    where TSend : IPacket, ITinyhandSerialize<TSend>
    where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        if (!this.netTerminal.IsActive)
        {
            return (NetResult.Closed, default, 0);
        }

        var responseTcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreatePacket(0, packet, out var owner);
        this.AddSendPacket(endPoint, owner, responseTcs);

        if (NetConstants.LogLowLevelNet)
        {
            // this.logger.TryGet(LogLevel.Debug)?.Log($"{this.netTerminal.NetTerminalString} to {endPoint.ToString()} {owner.Span.Length} {typeof(TSend).Name}/{typeof(TReceive).Name}");
        }

        try
        {
            var response = await this.netTerminal.Wait(responseTcs.Task, this.netTerminal.PacketTransmissionTimeout, default).ConfigureAwait(false);

            if (response.IsFailure)
            {
                return new(response.Result, default, 0);
            }

            TReceive? receive;
            try
            {
                receive = TinyhandSerializer.DeserializeObject<TReceive>(response.Received.Span.Slice(PacketHeader.Length));
            }
            catch
            {
                return new(NetResult.DeserializationFailed, default, 0);
            }
            finally
            {
                response.Return();
            }

            return (NetResult.Success, receive, (int)response.Additional);
        }
        catch
        {
            return (NetResult.Timeout, default, 0);
        }
    }*/

    internal void ProcessSend(NetSender netSender)
    {
        lock (this.items.SyncObject)
        {
            while (this.items.WaitingToSendListChain.First is { } item)
            {// Waiting to send
                if (!netSender.CanSend)
                {
                    return;
                }

                if (NetConstants.LogLowLevelNet)
                {
                    // this.logger.TryGet(LogLevel.Debug)?.Log($"{this.netTerminal.NetTerminalString} to {item.EndPoint.ToString()}, Send packet id:{item.PacketId}");
                }

                if (item.ResponseTcs is not null)
                {// WaitingToSend -> WaitingForResponse
                    netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner.IncrementAndShare());
                    item.SentMics = Mics.FastSystem;
                    this.items.WaitingToSendListChain.Remove(item);
                    this.items.WaitingForResponseListChain.AddLast(item);
                }
                else
                {// WaitingToSend -> Remove (without response)
                    netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner.IncrementAndShare());
                    item.Remove();
                }
            }

            while (this.items.WaitingForResponseListChain.First is { } item && (Mics.FastSystem - item.SentMics) > this.RetransmissionTimeoutMics)
            {// Waiting for response list
                if (!netSender.CanSend)
                {
                    return;
                }

                if (item.ResentCount >= this.MaxResendCount)
                {// The maximum number of resend attempts reached.
                    item.Remove();
                    continue;
                }

                // Reset packet id in order to improve the accuracy of RTT measurement.
                var newPacketId = RandomVault.Pseudo.NextUInt64();
                item.PacketIdValue = newPacketId;

                // PacketHeaderCode
                var span = item.MemoryOwner.Span;
                BitConverter.TryWriteBytes(span.Slice(10), newPacketId);
                BitConverter.TryWriteBytes(span.Slice(4), (uint)XxHash3.Hash64(span.Slice(8)));

                if (NetConstants.LogLowLevelNet)
                {
                    // this.logger.TryGet(LogLevel.Debug)?.Log($"{this.netTerminal.NetTerminalString} to {item.EndPoint.ToString()}, Resend packet id:{item.PacketId}");
                }

                netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner.IncrementAndShare());
                item.SentMics = Mics.FastSystem;
                item.ResentCount++;
                this.items.WaitingForResponseListChain.AddLast(item);
            }
        }
    }

    internal void ProcessReceive(NetEndpoint endpoint, ushort packetUInt16, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {// Checked: toBeShared.Length
        if (NetConstants.LogLowLevelNet)
        {
            // this.logger.TryGet(LogLevel.Debug)?.Log($"Receive actual");
        }

        // PacketHeaderCode
        var span = toBeShared.Span;
        if (BitConverter.ToUInt32(span.Slice(RelayHeader.RelayIdLength)) != (uint)XxHash3.Hash64(span.Slice(8)))
        {// Checksum
            return;
        }

        var packetType = (PacketType)packetUInt16;
        var packetId = BitConverter.ToUInt64(span.Slice(10));

        span = span.Slice(PacketHeader.Length);
        if (packetUInt16 < 127)
        {// Packet types (0-127), Client -> Server
            if (packetType == PacketType.Connect)
            {// ConnectPacket
                if (!this.netTerminal.IsActive)
                {
                    return;
                }
                else if (!this.netBase.NetOptions.EnableServer)
                {
                    return;
                }

                if (TinyhandSerializer.TryDeserialize<ConnectPacket>(span, out var p))
                {
                    if (p.ServerPublicKeyChecksum != this.netTerminal.NodePublicKey.GetHashCode())
                    {// Public Key does not match
                        return;
                    }

                    Task.Run(() =>
                    {
                        var packet = new ConnectPacketResponse(this.netBase.DefaultAgreement);
                        this.netTerminal.ConnectionTerminal.PrepareServerSide(endpoint, p, packet);
                        CreatePacket(packetId, packet, out var owner); // CreatePacketCode (no relay)
                        this.SendPacketWithoutRelay(endpoint, owner, default);
                    });

                    return;
                }
            }
            else if (packetType == PacketType.Ping)
            {// PingPacket
                if (this.netBase.NetOptions.EnableEssential)
                {
                    var packet = new PingPacketResponse(new(endpoint.EndPoint.Address, (ushort)endpoint.EndPoint.Port), this.netBase.NetOptions.NodeName);
                    CreatePacket(packetId, packet, out var owner); // CreatePacketCode (no relay)
                    this.SendPacketWithoutRelay(endpoint, owner, default);

                    if (NetConstants.LogLowLevelNet)
                    {
                        // this.logger.TryGet()?.Log($"{this.netTerminal.NetTerminalString} to {endPoint.ToString()} PingResponse");
                    }
                }

                return;
            }
            else if (packetType == PacketType.GetInformation)
            {// GetInformationPacket
                if (this.netBase.AllowUnsafeConnection)
                {
                    var packet = new GetInformationPacketResponse(this.netTerminal.NodePublicKey);
                    CreatePacket(packetId, packet, out var owner); // CreatePacketCode (no relay)
                    this.SendPacketWithoutRelay(endpoint, owner, default);
                }

                return;
            }
        }
        else if (packetUInt16 < 255)
        {// Packet response types (128-255), Server -> Client (Response)
            Item? item;
            lock (this.items.SyncObject)
            {
                if (this.items.PacketIdChain.TryGetValue(packetId, out item))
                {
                    item.Remove();
                }
            }

            if (item is not null)
            {
                if (NetConstants.LogLowLevelNet)
                {
                    this.logger.TryGet(LogLevel.Debug)?.Log($"{this.netTerminal.NetTerminalString} received {toBeShared.Span.Length} {packetType.ToString()}");
                }

                if (item.ResponseTcs is { } tcs)
                {
                    var elapsedMics = currentSystemMics > item.SentMics ? (int)(currentSystemMics - item.SentMics) : 0;
                    tcs.SetResult(new(NetResult.Success, 0, elapsedMics, toBeShared.IncrementAndShare()));
                }
            }
        }
        else
        {
        }
    }

    internal unsafe NetResult SendPacket(NetAddress netAddress, NetEndpoint netEndpoint, ByteArrayPool.MemoryOwner dataToBeMoved, TaskCompletionSource<NetResponse>? responseTcs, int relayNumber)
    {
        var length = dataToBeMoved.Span.Length;
        if (length < PacketHeader.Length ||
            length > NetConstants.MaxPacketLength)
        {
            return NetResult.InvalidData;
        }

        if (relayNumber > 0)
        {// The minimum number of relays
            if (this.netTerminal.RelayCircuit.NumberOfRelays < relayNumber)
            {
                return NetResult.InvalidRelay;
            }
        }
        else if (relayNumber < 0)
        {// The target relay
            if (this.netTerminal.RelayCircuit.NumberOfRelays < -relayNumber)
            {
                return NetResult.InvalidRelay;
            }
        }

        if (relayNumber == 0)
        {// No relay
            var span = dataToBeMoved.Span;
            BitConverter.TryWriteBytes(span, (ushort)0); // SourceRelayId
            span = span.Slice(sizeof(ushort));
            BitConverter.TryWriteBytes(span, netAddress.RelayId); // DestinationRelayId
        }
        else
        {// Relay
            if (!this.netTerminal.RelayCircuit.RelayKey.TryEncrypt(relayNumber, netAddress, dataToBeMoved.Span, out var encrypted, out netEndpoint))
            {
                dataToBeMoved.Return();
                return NetResult.InvalidRelay;
            }

            dataToBeMoved.Return();
            dataToBeMoved = encrypted;
        }

        var item = new Item(netEndpoint.EndPoint, dataToBeMoved, responseTcs);
        lock (this.items.SyncObject)
        {
            item.Goshujin = this.items;

            // Send immediately (This enhances performance in a local environment, but since it's meaningless in an actual network, it has been disabled)
            /*var netSender = this.netTerminal.NetSender;
            if (!item.Ack)
            {// Without ack
                netSender.SendImmediately(item.EndPoint, item.MemoryOwner.Span);
                item.Goshujin = null;
            }
            else
            {// Ack (sent list)
                netSender.SendImmediately(item.EndPoint, item.MemoryOwner.Span);
                item.SentMics = netSender.CurrentSystemMics;
                item.SentCount++;
                this.items.ToSendListChain.Remove(item);
                this.items.SentListChain.AddLast(item);
            }*/
        }

        // this.logger.TryGet(LogLevel.Debug)?.Log("AddSendPacket");

        return NetResult.Success;
    }

    internal unsafe NetResult SendPacketWithoutRelay(NetEndpoint endpoint, ByteArrayPool.MemoryOwner dataToBeMoved, TaskCompletionSource<NetResponse>? responseTcs)
    {
        var length = dataToBeMoved.Span.Length;
        if (length < PacketHeader.Length ||
            length > NetConstants.MaxPacketLength)
        {
            return NetResult.InvalidData;
        }

        var span = dataToBeMoved.Span;
        BitConverter.TryWriteBytes(span, (ushort)0); // SourceRelayId
        span = span.Slice(sizeof(ushort));
        BitConverter.TryWriteBytes(span, endpoint.RelayId); // DestinationRelayId
        Console.WriteLine("sss" + endpoint.ToString());

        var item = new Item(endpoint.EndPoint, dataToBeMoved, responseTcs);
        lock (this.items.SyncObject)
        {
            item.Goshujin = this.items;
        }

        return NetResult.Success;
    }

    private static void CreatePacket<TPacket>(ulong packetId, TPacket packet, out ByteArrayPool.MemoryOwner owner)
        where TPacket : IPacket, ITinyhandSerialize<TPacket>
    {
        if (packetId == 0)
        {
            packetId = RandomVault.Pseudo.NextUInt64();
        }

        var arrayOwner = PacketPool.Rent();
        var writer = new TinyhandWriter(arrayOwner.ByteArray);

        // PacketHeaderCode
        scoped Span<byte> header = stackalloc byte[PacketHeader.Length];
        var span = header;

        BitConverter.TryWriteBytes(span, 0u); // SourceRelayId/DestinationRelayId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, 0u); // Hash
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)TPacket.PacketType); // PacketType
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, packetId); // Id
        span = span.Slice(sizeof(ulong));

        writer.WriteSpan(header);

        TinyhandSerializer.SerializeObject(ref writer, packet);

        writer.FlushAndGetArray(out var array, out var arrayLength, out var isInitialBuffer);
        writer.Dispose();

        // Get checksum
        span = array.AsSpan(0, arrayLength);
        BitConverter.TryWriteBytes(span.Slice(4), (uint)XxHash3.Hash64(span.Slice(8)));

        if (!isInitialBuffer)
        {
            arrayOwner = new(array);
        }

        owner = arrayOwner.ToMemoryOwner(0, arrayLength);
    }
}
