// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tinyhand.IO;

#pragma warning disable SA1204

namespace Netsphere.Packet;

public sealed partial class PacketTerminal
{
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private sealed partial class Item
    {
        [Link(Type = ChainType.QueueList, Name = "SendQueue")]
        public Item(ulong packetId, ByteArrayPool.MemoryOwner dataToBeMoved)
        {
            this.PacketId = packetId;
            this.dataToBeMoved = dataToBeMoved;
        }

        [Link(Type = ChainType.Unordered, AddValue = false)]
        public ulong PacketId { get; }

        private ByteArrayPool.MemoryOwner dataToBeMoved;
        private ulong sentMics;
        private int sentCount;
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
        this.TryAdd(owner);
    }

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend packet)
        where TSend : IPacket, ITinyhandSerialize<TSend>
        where TReceive : IPacket, ITinyhandSerialize<TReceive>
    {
        var tcs = new TaskCompletionSource<(NetResult Result, ByteArrayPool.MemoryOwner ToBeMoved)>();
        CreatePacket(0, packet, out var owner);
        this.TryAdd(owner);

        var task = tcs.Task.Result;
        if (task.Result != NetResult.Success)
        {
            return new(task.Result, default);
        }

        TReceive? receive;
        try
        {
            receive = TinyhandSerializer.DeserializeObject<TReceive>(task.ToBeMoved.Span.Slice(PacketService.PacketHeaderSize));
        }
        catch
        {
            return new(NetResult.DeserializationError, default);
        }

        task.ToBeMoved.Return();
        return (NetResult.Success, receive);
    }

    internal void ProcessReceive(IPEndPoint endPoint, Span<byte> packet, long currentMics)
    {
        if (packet.Length < PacketService.PacketHeaderSize)
        {
            return;
        }

        var header = Unsafe.ReadUnaligned<PacketHeader>(ref MemoryMarshal.GetReference(packet));
    }

    private bool TryAdd(ByteArrayPool.MemoryOwner dataToBeMoved)
    {
        if (dataToBeMoved.Span.Length > NetControl.MaxPacketLength)
        {
            return false;
        }

        var item = new Item(dataToBeMoved);
        lock (this.items.SyncObject)
        {

            item.Goshujin = this.items;
        }

        return true;
    }

    private static void CreatePacket<TPacket>(uint packetId, TPacket packet, out ByteArrayPool.MemoryOwner owner)
        where TPacket : IPacket, ITinyhandSerialize<TPacket>
    {
        PacketHeader header = default;
        header.PacketId = packetId == 0 ? RandomVault.Pseudo.NextUInt32() : packetId;
        header.PacketType = TPacket.PacketType;
        header.DataSize = 0;

        var arrayOwner = PacketPool.Rent();
        var writer = new TinyhandWriter(arrayOwner.ByteArray);
        var packetHeaderSpan = writer.GetSpan(PacketService.PacketHeaderSize);
        writer.Advance(PacketService.PacketHeaderSize);

        var written = writer.Written;
        TinyhandSerializer.SerializeObject(ref writer, packet);

        header.DataSize = (ushort)(writer.Written - written);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(packetHeaderSpan), header);

        writer.FlushAndGetArray(out var array, out var arrayLength, out var isInitialBuffer);
        writer.Dispose();

        if (!isInitialBuffer)
        {
            arrayOwner = new(array);
        }

        owner = arrayOwner.ToMemoryOwner(0, arrayLength);
    }
}
