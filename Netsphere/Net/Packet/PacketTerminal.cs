// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Netsphere.Net;
using Tinyhand.IO;

#pragma warning disable SA1204

namespace Netsphere.Packet;

public sealed partial class PacketTerminal
{
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private sealed partial class Item
    {
        [Link(Type = ChainType.QueueList, Name = "SendQueue")]
        public Item(IPEndPoint endPoint, ByteArrayPool.MemoryOwner dataToBeMoved, TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>? tcs)
        {
            if (dataToBeMoved.Span.Length < sizeof(ulong))
            {
                throw new InvalidOperationException();
            }

            this.endPoint = endPoint;
            this.PacketId = BitConverter.ToUInt64(dataToBeMoved.Span) & 0xFFFF_FFFF_FFFF_FF00;
            this.dataToBeMoved = dataToBeMoved;
            this.Tcs = tcs;
        }

        [Link(Type = ChainType.Unordered, AddValue = false)]
        public ulong PacketId { get; }

        public TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>? Tcs { get; }

        private readonly IPEndPoint endPoint;
        private readonly ByteArrayPool.MemoryOwner dataToBeMoved;
        private long sentMics;
        private int sentCount;

        public void ProcessSend(NetSender netSender)
        {
            netSender.Send(this.endPoint, this.dataToBeMoved.Span);
        }

        public void Return()
        {
            this.dataToBeMoved.Return();
        }
    }

    public PacketTerminal(NetTerminal netTerminal)
    {
        this.netTerminal = netTerminal;
    }

    private readonly NetTerminal netTerminal;
    private readonly Item.GoshujinClass items = new();

    public void SendAndForget<TSend>(IPEndPoint endPoint, TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
    {
        CreatePacket(0, packet, out var owner);
        this.TryAdd(endPoint, owner, default);
    }

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(IPEndPoint endPoint, TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
        where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        var tcs = new TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>();
        CreatePacket(0, packet, out var owner);
        this.TryAdd(endPoint, owner, tcs);

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
                receive = TinyhandSerializer.DeserializeObject<TReceive>(task.ToBeMoved.Span.Slice(sizeof(ulong)));
            }
            catch
            {
                return new(NetResult.DeserializationError, default);
            }

            task.ToBeMoved.Return();
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
            if (this.items.SendQueueChain.TryPeek(out var item))
            {
                item.ProcessSend(netSender);
            }
        }
    }

    internal void ProcessReceive(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {
        if (toBeShared.Span.Length < sizeof(ulong))
        {
            return;
        }

        var header = BitConverter.ToUInt64(toBeShared.Span);
        var packetId = header & 0xFFFF_FFFF_FFFF_FF00;
        var packetType = (PacketType)(header & 0xFF);
        if ((header & 0x80) != 0)
        {// Reponse
            Item? item;
            lock (this.items.SyncObject)
            {
                if (this.items.PacketIdChain.TryGetValue(packetId, out item))
                {
                    item.Goshujin = null;
                }
            }

            if (item is not null)
            {
                item.Return();
                if (item.Tcs is not null)
                {
                    item.Tcs.SetResult((NetResult.Success, toBeShared.IncrementAndShare()));
                }
            }
        }
        else
        {
            if (packetType == PacketType.Ping)
            {
                var packet = new PacketPingResponse(new(endPoint.Address, (ushort)endPoint.Port), this.netTerminal.NetBase.NodeName);
                CreatePacket(packetId, packet, out var owner);
                this.TryAdd(endPoint, owner, default);
            }
        }
    }

    private bool TryAdd(IPEndPoint endPoint, ByteArrayPool.MemoryOwner dataToBeMoved, TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>? tcs)
    {
        if (dataToBeMoved.Span.Length > NetControl.MaxPacketLength)
        {
            return false;
        }

        var item = new Item(endPoint, dataToBeMoved, tcs);
        lock (this.items.SyncObject)
        {
            item.Goshujin = this.items;
        }

        return true;
    }

    private static void CreatePacket<TPacket>(ulong packetId, TPacket packet, out ByteArrayPool.MemoryOwner owner)
        where TPacket : IPacket, ITinyhandSerialize<TPacket>
    {
        if (packetId == 0)
        {
            packetId = RandomVault.Pseudo.NextUInt64();
        }

        var header = (packetId & 0xFFFF_FFFF_FFFF_FF00) | (ulong)TPacket.PacketType;
        var arrayOwner = PacketPool.Rent();
        var writer = new TinyhandWriter(arrayOwner.ByteArray);
        var span = writer.GetSpan(sizeof(ulong));
        BitConverter.TryWriteBytes(span, header);
        writer.Advance(sizeof(ulong));
        TinyhandSerializer.SerializeObject(ref writer, packet);

        writer.FlushAndGetArray(out var array, out var arrayLength, out var isInitialBuffer);
        writer.Dispose();

        if (!isInitialBuffer)
        {
            arrayOwner = new(array);
        }

        owner = arrayOwner.ToMemoryOwner(0, arrayLength);
    }
}
