// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;
using Netsphere.Stats;
using Tinyhand.IO;

#pragma warning disable SA1204

namespace Netsphere.Packet;

public sealed partial class PacketTerminal
{
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private sealed partial class Item
    {
        [Link(Type = ChainType.LinkedList, Name = "ToSendList", AutoLink = true)]
        [Link(Type = ChainType.LinkedList, Name = "SentList", AutoLink = false)]
        public Item(IPEndPoint endPoint, ByteArrayPool.MemoryOwner dataToBeMoved, bool ack, TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>? tcs)
        {
            if (dataToBeMoved.Span.Length < PacketHeader.Length)
            {
                throw new InvalidOperationException();
            }

            this.EndPoint = endPoint;
            this.Ack = ack;
            this.PacketId = BitConverter.ToUInt64(dataToBeMoved.Span.Slice(8)); // PacketHeaderCode
            this.MemoryOwner = dataToBeMoved;
            this.Tcs = tcs;
        }

        [Link(Primary = true, Type = ChainType.Unordered, AddValue = false)]
        public ulong PacketId { get; }

        public IPEndPoint EndPoint { get; }

        public bool Ack { get; }

        public ByteArrayPool.MemoryOwner MemoryOwner { get; }

        public TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>? Tcs { get; }

        public long SentMics { get; set; }

        public int SentCount { get; set; }

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

        this.ResendIntervalMics = Mics.FromMilliseconds(500);
        this.SendCountLimit = 3;
    }

    public long ResendIntervalMics { get; set; }

    public int SendCountLimit { get; set; }

    private readonly NetBase netBase;
    private readonly NetStats netStats;
    private readonly NetTerminal netTerminal;
    private readonly ILogger logger;
    private readonly Item.GoshujinClass items = new();

    public void SendAndForget<TSend>(NetAddress address, TSend packet)
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
    }

    public Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(NetAddress address, TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
        where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        if (!this.netTerminal.TryCreateEndPoint(in address, out var endPoint))
        {
            return Task.FromResult<(NetResult Result, TReceive? Value)>((NetResult.InvalidAddress, default));
        }

        return this.SendAndReceiveAsync<TSend, TReceive>(endPoint, packet);
    }

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(NetEndPoint endPoint, TSend packet)
    where TSend : IPacket, ITinyhandSerialize<TSend>
    where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        if (this.netTerminal.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Timeout, default);
        }

        var tcs = new TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreatePacket(0, packet, out var owner);
        this.AddSendPacket(endPoint.EndPoint, owner, true, tcs);

        try
        {
            var task = await tcs.Task.WaitAsync(this.netTerminal.ResponseTimeout, this.netTerminal.CancellationToken).ConfigureAwait(false);

            if (task.Result != NetResult.Success)
            {
                task.ToBeMoved.Return();
                return new(task.Result, default);
            }

            TReceive? receive;
            try
            {
                receive = TinyhandSerializer.DeserializeObject<TReceive>(task.ToBeMoved.Span.Slice(PacketHeader.Length));
            }
            catch
            {
                return new(NetResult.DeserializationError, default);
            }
            finally
            {
                task.ToBeMoved.Return();
            }

            return (NetResult.Success, receive);
        }
        catch
        {
            return (NetResult.Timeout, default);
        }
    }

    internal void ProcessSend(NetSender netSender)
    {
        lock (this.items.SyncObject)
        {
            // this.logger.TryGet()?.Log($"{this.netTerminal.NetTerminalString} ProcessSend() - {this.items.ToSendListChain.Count}");

            while (this.items.ToSendListChain.First is { } item)
            {// To send list
                if (!netSender.CanSend)
                {
                    return;
                }

                if (!item.Ack)
                {// Without ack
                    netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner);
                    item.Remove();
                }
                else
                {// Ack (sent list)
                    netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner);
                    item.SentMics = netSender.CurrentSystemMics;
                    item.SentCount++;
                    this.items.ToSendListChain.Remove(item);
                    this.items.SentListChain.AddLast(item);
                }
            }

            while (this.items.SentListChain.First is { } item && (netSender.CurrentSystemMics - item.SentMics) > this.ResendIntervalMics)
            {// Sent list
                if (!netSender.CanSend)
                {
                    return;
                }

                if (item.SentCount >= this.SendCountLimit)
                {
                    item.Remove();
                    continue;
                }

                netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner);
                item.SentMics = netSender.CurrentSystemMics;
                item.SentCount++;
                this.items.SentListChain.AddLast(item);
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
                        this.AddSendPacket(endPoint, owner, false, default);
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
                    this.AddSendPacket(endPoint, owner, false, default);
                    return;
                }
                else if (packetType == PacketType.GetInformation)
                {// PacketGetInformation
                    var packet = new PacketGetInformationResponse(this.netBase.NodePublicKey);
                    CreatePacket(packetId, packet, out var owner);
                    this.AddSendPacket(endPoint, owner, false, default);
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

                if (item.Tcs is not null)
                {
                    item.Tcs.SetResult((NetResult.Success, toBeShared.IncrementAndShare()));
                }
            }
        }
        else
        {
        }
    }

    internal unsafe void AddSendPacket(IPEndPoint endPoint, ByteArrayPool.MemoryOwner dataToBeMoved, bool ack, TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>? tcs)
    {
        if (dataToBeMoved.Span.Length > NetControl.MaxPacketLength)
        {
            return;
        }

        var item = new Item(endPoint, dataToBeMoved, ack, tcs);
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
