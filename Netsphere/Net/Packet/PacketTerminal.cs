// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;
using Tinyhand.IO;

#pragma warning disable SA1204

namespace Netsphere.Packet;

public sealed partial class PacketTerminal
{
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private sealed partial class Item
    {
        [Link(Type = ChainType.QueueList, Name = "SendQueue")]
        public Item(ByteArrayPool.MemoryOwner dataToBeMoved, TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>? tcs)
        {
            if (dataToBeMoved.Span.Length < sizeof(ulong))
            {
                throw new InvalidOperationException();
            }

            this.PacketId = BitConverter.ToUInt64(dataToBeMoved.Span);
            this.dataToBeMoved = dataToBeMoved;
            this.Tcs = tcs;
        }

        [Link(Type = ChainType.Unordered, AddValue = false)]
        public ulong PacketId { get; }

        public TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>? Tcs { get; }

        private readonly ByteArrayPool.MemoryOwner dataToBeMoved;
        private long sentMics;
        private int sentCount;

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

    public void SendAndForget<TSend>(TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
    {
        CreatePacket(0, packet, out var owner);
        this.TryAdd(owner, default);
    }

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
        where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        var tcs = new TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>();
        CreatePacket(0, packet, out var owner);
        this.TryAdd(owner, tcs);

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

    internal void ProcessSend(long currentSystemMics)
    {

    }

    internal void ProcessReceive(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared, long currentSystemMics)
    {
        if (toBeShared.Span.Length < sizeof(ulong))
        {
            return;
        }

        var header = BitConverter.ToUInt64(toBeShared.Span);
        var packetType = (PacketType)(header & 0xFF);
        if ((header & 0x80) != 0)
        {// Reponse
            Item? item;
            lock (this.items.SyncObject)
            {
                if (this.items.PacketIdChain.TryGetValue(header, out item))
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
            }
        }
    }

    private bool TryAdd(ByteArrayPool.MemoryOwner dataToBeMoved, TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>? tcs)
    {
        if (dataToBeMoved.Span.Length > NetControl.MaxPacketLength)
        {
            return false;
        }

        var item = new Item(dataToBeMoved, tcs);
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
        writer.Write(header);
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
