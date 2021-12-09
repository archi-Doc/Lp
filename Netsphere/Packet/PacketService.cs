// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Netsphere;

internal static class PacketService
{
    static PacketService()
    {
        HeaderSize = Marshal.SizeOf(default(PacketHeader));
        DataHeaderSize = Marshal.SizeOf(default(DataHeader));
        // PacketInfo = new PacketInfo[] { new(typeof(PacketPunch), 0, false), };

        var relay = new PacketRelay();
        relay.NextEndpoint = new(IPAddress.IPv6Loopback, NetControl.MaxPort);
        RelayPacketSize = Tinyhand.TinyhandSerializer.Serialize(relay).Length;
        SafeMaxPacketSize = NetControl.MaxPayload - HeaderSize - DataHeaderSize - RelayPacketSize;
        SafeMaxPacketSize -= 8; // Safety margin
        DataPacketSize = 1369;
    }

    public static int HeaderSize { get; }

    public static int DataHeaderSize { get; }

    public static int RelayPacketSize { get; }

    public static int SafeMaxPacketSize { get; }

    public static int DataPacketSize { get; }

    // public static PacketInfo[] PacketInfo;

    private const int InitialBufferLength = 2048;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsManualAck(PacketId id) => id switch
    {// Manual ack (only for unencrypted transfer)
        PacketId.Encrypt => true,
        PacketId.Punch => true,
        PacketId.PunchResponse => true,
        PacketId.Ping => true,
        PacketId.PingResponse => true,
        _ => false,
    };

    internal static unsafe void InsertGene(Memory<byte> memory, ulong gene)
    {
        fixed (byte* pb = memory.Span)
        {
            (*(PacketHeader*)pb).Gene = gene;
        }
    }

    internal static unsafe void InsertDataSize(Memory<byte> memory, ushort size)
    {
        fixed (byte* pb = memory.Span)
        {
            (*(PacketHeader*)pb).DataSize = size;
        }
    }

    internal static (int NumberOfGenes, int DataSize, int LastDataSize) GetDataInfo(int totalSize)
    {
        var numberOfGenes = totalSize / PacketService.DataPacketSize;
        var lastDataSize = totalSize - (numberOfGenes * PacketService.DataPacketSize);
        if (lastDataSize > 0)
        {
            numberOfGenes++;
        }

        return (numberOfGenes, PacketService.DataPacketSize, lastDataSize);
    }

    internal static unsafe ReadOnlyMemory<byte> GetDataMemory(ReadOnlyMemory<byte> memory)
    {
        if (memory.Length < DataHeaderSize)
        {
            return memory;
        }

        var span = memory.Span;
        DataHeader dataHeader = default;
        fixed (byte* pb = span)
        {
            dataHeader = *(DataHeader*)pb;
        }

        var dataMemory = memory.Slice(DataHeaderSize);
        if (!dataHeader.ChecksumEquals(Arc.Crypto.FarmHash.Hash64(dataMemory.Span)))
        {
            return memory;
        }
        else
        {
            return dataMemory;
        }
    }

    internal static unsafe bool GetData(ref PacketHeader header, ref ByteArrayPool.MemoryOwner owner)
    {
        if (header.Id != PacketId.Data)
        {// Not PacketData
            return false;
        }
        else if (owner.Memory.Length < DataHeaderSize)
        {
            return false;
        }

        var span = owner.Memory.Span;
        DataHeader dataHeader = default;
        fixed (byte* pb = span)
        {
            dataHeader = *(DataHeader*)pb;
        }

        span = span.Slice(DataHeaderSize);
        if (!dataHeader.ChecksumEquals(Arc.Crypto.FarmHash.Hash64(span)))
        {
            return false;
        }

        header.Id = dataHeader.PacketId;
        owner = owner.Slice(DataHeaderSize);
        return true;
    }

    internal static unsafe void CreatePacket(ref PacketHeader header, PacketId packetId, ulong id, ReadOnlySpan<byte> data, out ByteArrayPool.MemoryOwner owner)
    {// PacketHeader, DataHeader, Data
        if (data.Length > PacketService.SafeMaxPacketSize)
        {
            throw new ArgumentOutOfRangeException();
        }

        var arrayOwner = PacketPool.Rent();
        var size = PacketService.HeaderSize + PacketService.DataHeaderSize + data.Length;
        var span = arrayOwner.ByteArray.AsSpan();

        fixed (byte* pb = span)
        {
            header.Id = PacketId.Data;
            header.DataSize = (ushort)(PacketService.DataHeaderSize + data.Length);
            *(PacketHeader*)pb = header;
        }

        span = span.Slice(PacketService.HeaderSize);
        var dataHeader = new DataHeader(id, packetId, Arc.Crypto.FarmHash.Hash64(data));
        fixed (byte* pb = span)
        {
            *(DataHeader*)pb = dataHeader;
        }

        span = span.Slice(PacketService.DataHeaderSize);
        data.CopyTo(span);

        owner = arrayOwner.ToMemoryOwner(0, size);
    }

    internal static unsafe void CreatePacket<T>(ref PacketHeader header, T value, PacketId rawPacketId, out ByteArrayPool.MemoryOwner owner)
    {
        var arrayOwner = PacketPool.Rent();
        var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
        var packetHeaderSpan = writer.GetSpan(PacketService.HeaderSize);
        writer.Advance(PacketService.HeaderSize);

        var written = writer.Written;
        TinyhandSerializer.Serialize(ref writer, value);

        fixed (byte* pb = packetHeaderSpan)
        {
            header.Id = rawPacketId;
            header.DataSize = (ushort)(writer.Written - written);
            *(PacketHeader*)pb = header;
        }

        writer.FlushAndGetArray(out var array, out var arrayLength);
        if (array != arrayOwner.ByteArray)
        {
            arrayOwner = new(array);
        }

        owner = arrayOwner.ToMemoryOwner(0, arrayLength);
        writer.Dispose();
    }

    internal static unsafe void CreateAckAndPacket<T>(ref PacketHeader header, ulong secondGene, T value, PacketId rawPacketId, out ByteArrayPool.MemoryOwner owner)
    {
        var arrayOwner = PacketPool.Rent();
        var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
        var span = writer.GetSpan(PacketService.HeaderSize * 2);
        writer.Advance(PacketService.HeaderSize * 2);

        var written = writer.Written;
        TinyhandSerializer.Serialize(ref writer, value);

        fixed (byte* pb = span)
        {
            (*(PacketHeader*)pb).Engagement = header.Engagement;
            (*(PacketHeader*)pb).Id = PacketId.Ack;
            (*(PacketHeader*)pb).DataSize = 0;
            (*(PacketHeader*)pb).Gene = header.Gene;

            header.Id = rawPacketId;
            header.DataSize = (ushort)(writer.Written - written);
            header.Gene = secondGene;
            *(PacketHeader*)(pb + PacketService.HeaderSize) = header;
        }

        writer.FlushAndGetArray(out var array, out var arrayLength);
        if (array != arrayOwner.ByteArray)
        {
            arrayOwner = new(array);
        }

        owner = arrayOwner.ToMemoryOwner(0, arrayLength);
        writer.Dispose();
    }
}
