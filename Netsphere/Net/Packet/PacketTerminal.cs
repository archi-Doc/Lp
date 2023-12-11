// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;
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
            this.PacketId = BitConverter.ToUInt64(dataToBeMoved.Span.Slice(8)); // PacketHeaderCode
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

        this.InitialResendTimeoutMics = Mics.FromMilliseconds(500);
        this.MaxResendCount = 2;
    }

    public long InitialResendTimeoutMics { get; set; }

    public int MaxResendCount { get; set; }

    private readonly NetBase netBase;
    private readonly NetStats netStats;
    private readonly NetTerminal netTerminal;
    private readonly ILogger logger;
    private readonly Item.GoshujinClass items = new();

    /*public void SendAndForget<TSend>(NetAddress address, TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
    {
        if (!this.netTerminal.TryCreateEndPoint(in address, out var endPoint))
        {
            return;
        }

        this.SendAndForget(endPoint, packet);
    }

    public void SendAndForget<TSend>(NetEndPoint endPoint, TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
    {
        CreatePacket(0, packet, out var owner);
        this.AddSendPacket(endPoint.EndPoint, owner, true, default);
    }*/

    public Task<(NetResult Result, TReceive? Value, int RttMics)> SendAndReceiveAsync<TSend, TReceive>(NetAddress address, TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
        where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        if (!this.netTerminal.TryCreateEndPoint(in address, out var endPoint))
        {
            return Task.FromResult<(NetResult, TReceive?, int)>((NetResult.InvalidAddress, default, 0));
        }

        return this.SendAndReceiveAsync<TSend, TReceive>(endPoint, packet);
    }

    public async Task<(NetResult Result, TReceive? Value, int RttMics)> SendAndReceiveAsync<TSend, TReceive>(NetEndPoint endPoint, TSend packet)
    where TSend : IPacket, ITinyhandSerialize<TSend>
    where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        if (this.netTerminal.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Timeout, default, 0);
        }

        var responseTcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreatePacket(0, packet, out var owner);
        this.AddSendPacket(endPoint.EndPoint, owner, responseTcs);

        try
        {
            var response = await responseTcs.Task.WaitAsync(this.netTerminal.ResponseTimeout, this.netTerminal.CancellationToken).ConfigureAwait(false);

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
                return new(NetResult.DeserializationError, default, 0);
            }
            finally
            {
                response.Return();
            }

            return (NetResult.Success, receive, response.ElapsedMics);
        }
        catch
        {
            return (NetResult.Timeout, default, 0);
        }
    }

    internal void ProcessSend(NetSender netSender)
    {
        lock (this.items.SyncObject)
        {
            // this.logger.TryGet()?.Log($"{this.netTerminal.NetTerminalString} ProcessSend() - {this.items.ToSendListChain.Count}");

            while (this.items.WaitingToSendListChain.First is { } item)
            {// Waiting to send
                if (!netSender.CanSend)
                {
                    return;
                }

                if (item.ResponseTcs is not null)
                {// WaitingToSend -> WaitingForResponse
                    netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner);
                    item.SentMics = netSender.CurrentSystemMics;
                    this.items.WaitingToSendListChain.Remove(item);
                    this.items.WaitingForResponseListChain.AddLast(item);
                }
                else
                {// WaitingToSend -> Remove (without response)
                    netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner);
                    item.Remove();
                }
            }

            while (this.items.WaitingForResponseListChain.First is { } item && (netSender.CurrentSystemMics - item.SentMics) > this.InitialResendTimeoutMics)
            {// Sent list
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
                BitConverter.TryWriteBytes(span.Slice(8), newPacketId);
                BitConverter.TryWriteBytes(span, (uint)XxHash3.Hash64(span.Slice(4)));

                netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner);
                item.SentMics = netSender.CurrentSystemMics;
                item.ResentCount++;
                this.items.WaitingForResponseListChain.AddLast(item);
            }
        }
    }

    internal void ProcessReceive(IPEndPoint endPoint, ushort packetUInt16, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {
        // PacketHeaderCode
        var span = toBeShared.Span;
        if (BitConverter.ToUInt32(span) != (uint)XxHash3.Hash64(span.Slice(4)))
        {// Checksum
            return;
        }

        var packetType = (PacketType)packetUInt16;
        var packetId = BitConverter.ToUInt64(span.Slice(8));

        span = span.Slice(PacketHeader.Length);
        if (packetUInt16 < 127)
        {// Packet types (0-127)
            if (packetType == PacketType.Connect)
            {// PacketConnect
                if (TinyhandSerializer.TryDeserialize<PacketConnect>(span, out var p))
                {
                    Task.Run(() =>
                    {
                        var packet = new PacketConnectResponse(this.netBase.ServerOptions);

                        this.netTerminal.ConnectionTerminal.PrepareServerSide(new(endPoint, p.Engagement), p, packet);
                        CreatePacket(packetId, packet, out var owner);
                        this.AddSendPacket(endPoint, owner, default);
                    });

                    return;
                }
            }
            else if (this.netBase.NetsphereOptions.EnableEssential)
            {
                if (packetType == PacketType.Ping)
                {// PacketPing
                    this.logger.TryGet(LogLevel.Debug)?.Log($"{this.netTerminal.NetTerminalString} to {endPoint.ToString()} PacketPingResponse");

                    var packet = new PacketPingResponse(new(endPoint.Address, (ushort)endPoint.Port), this.netTerminal.NetBase.NodeName);
                    CreatePacket(packetId, packet, out var owner);
                    this.AddSendPacket(endPoint, owner, default);
                    return;
                }
                else if (packetType == PacketType.GetInformation)
                {// PacketGetInformation
                    var packet = new PacketGetInformationResponse(this.netBase.NodePublicKey);
                    CreatePacket(packetId, packet, out var owner);
                    this.AddSendPacket(endPoint, owner, default);
                    return;
                }
            }
        }
        else if (packetUInt16 < 255)
        {// Packet response types (128-255)
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
                this.logger.TryGet(LogLevel.Debug)?.Log($"{this.netTerminal.NetTerminalString}, Received {toBeShared.Span.Length}");

                if (item.ResponseTcs is not null)
                {
                    var elapsedMics = currentSystemMics > item.SentMics ? (int)(currentSystemMics - item.SentMics) : 0;
                    item.ResponseTcs.SetResult(new(NetResult.Success, toBeShared.IncrementAndShare(), elapsedMics));
                }
            }
        }
        else
        {
        }
    }

    internal unsafe void AddSendPacket(IPEndPoint endPoint, ByteArrayPool.MemoryOwner dataToBeMoved, TaskCompletionSource<NetResponse>? responseTcs)
    {
        if (dataToBeMoved.Span.Length > NetControl.MaxPacketLength)
        {
            return;
        }

        var item = new Item(endPoint, dataToBeMoved, responseTcs);
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

        BitConverter.TryWriteBytes(span, 0u); // Hash
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)0); // Engagement
        span = span.Slice(sizeof(ushort));

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
        BitConverter.TryWriteBytes(span, (uint)XxHash3.Hash64(span.Slice(4)));

        if (!isInitialBuffer)
        {
            arrayOwner = new(array);
        }

        owner = arrayOwner.ToMemoryOwner(0, arrayLength);
    }
}
