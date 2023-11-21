// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Netsphere.Packet;

namespace Netsphere;

internal static class PacketService
{
    static PacketService()
    {
        PacketHeaderSize = Marshal.SizeOf(default(PacketHeader));
        // GeneHeaderSize = Marshal.SizeOf(default(GenePacketHeader));

        HeaderSizeObsolete = Marshal.SizeOf(default(PacketHeaderObsolete));
        DataHeaderSizeObsolete = Marshal.SizeOf(default(DataHeader));
        DataFollowingHeaderSizeObsolete = Marshal.SizeOf(default(DataFollowingHeader));
        // PacketInfo = new PacketInfo[] { new(typeof(PacketPunch), 0, false), };

        var relay = new PacketRelayObsolete();
        relay.NextEndpoint = new(IPAddress.IPv6Loopback, NetControl.MaxPort);
        RelayPacketSize = Tinyhand.TinyhandSerializer.Serialize(relay).Length;
        SafeMaxPayloadSize = NetControl.MaxPacketLength - HeaderSizeObsolete - DataHeaderSizeObsolete - RelayPacketSize;
        SafeMaxPayloadSize = ((SafeMaxPayloadSize / 16) * 16) - 1; // -1 for PKCS7 padding.

        DataFollowingPayloadSize = 1375; // = SafeMaxPayloadSize
        DataPayloadSize = DataFollowingPayloadSize - DataHeaderSizeObsolete + DataFollowingHeaderSizeObsolete;
    }

    public static int PacketHeaderSize { get; }

    public static int GeneHeaderSize { get; }

    public static int HeaderSizeObsolete { get; }

    public static int DataHeaderSizeObsolete { get; }

    public static int DataFollowingHeaderSizeObsolete { get; }

    public static int RelayPacketSize { get; }

    public static int SafeMaxPayloadSize { get; }

    public static int DataPayloadSize { get; }

    public static int DataFollowingPayloadSize { get; }

    // public static PacketInfo[] PacketInfo;

    private const int InitialBufferLength = 2048;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsManualAck(PacketIdObsolete packetId) => packetId switch
    {// Manual ack (only for unencrypted transfer)
        PacketIdObsolete.Encrypt => true,
        PacketIdObsolete.Punch => true,
        PacketIdObsolete.PunchResponse => true,
        PacketIdObsolete.Ping => true,
        PacketIdObsolete.PingResponse => true,
        PacketIdObsolete.GetNodeInformation => true,
        PacketIdObsolete.GetNodeInformationResponse => true,
        _ => false,
    };

    internal static unsafe void InsertGene(Memory<byte> memory, ulong gene)
    {
        fixed (byte* pb = memory.Span)
        {
            (*(PacketHeaderObsolete*)pb).Gene = gene;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void InsertDataSize(Memory<byte> memory, ushort size)
    {
        fixed (byte* pb = memory.Span)
        {
            (*(PacketHeaderObsolete*)pb).DataSize = size;
        }
    }

    internal static (int NumberOfGenes, int FirstDataSize, int FollowingDataSize, int LastDataSize) GetDataSize(int totalSize)
    {
        var remaining = totalSize;
        if (remaining <= PacketService.DataPayloadSize)
        {
            return (1, PacketService.DataPayloadSize, PacketService.DataFollowingPayloadSize, 0);
        }

        remaining -= PacketService.DataPayloadSize;
        var n = remaining / PacketService.DataFollowingPayloadSize;
        var lastDataSize = remaining - (n * PacketService.DataFollowingPayloadSize);
        if (lastDataSize > 0)
        {
            n++;
        }

        return (n + 1, PacketService.DataPayloadSize, PacketService.DataFollowingPayloadSize, lastDataSize);
    }

    internal static unsafe (ulong DataId, PacketIdObsolete PacketId, ByteArrayPool.MemoryOwner DataMemory) GetData(ByteArrayPool.MemoryOwner owner)
    {
        if (owner.Memory.Length < DataHeaderSizeObsolete)
        {
            return (0, PacketIdObsolete.Invalid, default);
        }

        var span = owner.Memory.Span;
        DataHeader dataHeader = default;
        fixed (byte* pb = span)
        {
            dataHeader = *(DataHeader*)pb;
        }

        var dataMemory = owner.Slice(DataHeaderSizeObsolete);
        if (!dataHeader.ChecksumEquals(Arc.Crypto.FarmHash.Hash64(dataMemory.Memory.Span)))
        {
            return (dataHeader.DataId, dataHeader.PacketId, owner);
        }
        else
        {
            return (dataHeader.DataId, dataHeader.PacketId, dataMemory);
        }
    }

    internal static unsafe ReadOnlyMemory<byte> GetDataFollowing(ReadOnlyMemory<byte> memory)
    {
        if (memory.Length < DataHeaderSizeObsolete)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var span = memory.Span;
        DataFollowingHeader dataHeader = default;
        fixed (byte* pb = span)
        {
            dataHeader = *(DataFollowingHeader*)pb;
        }

        var dataMemory = memory.Slice(DataFollowingHeaderSizeObsolete);
        if (!dataHeader.ChecksumEquals(Arc.Crypto.FarmHash.Hash64(dataMemory.Span)))
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        else
        {
            return dataMemory;
        }
    }

    internal static unsafe bool GetData(ref PacketHeaderObsolete header, ref ByteArrayPool.MemoryOwner owner)
    {
        if (header.Id != PacketIdObsolete.Data)
        {// Not PacketData
            return false;
        }
        else if (owner.Memory.Length < DataHeaderSizeObsolete)
        {
            return false;
        }

        var span = owner.Memory.Span;
        DataHeader dataHeader = default;
        fixed (byte* pb = span)
        {
            dataHeader = *(DataHeader*)pb;
        }

        span = span.Slice(DataHeaderSizeObsolete);
        if (!dataHeader.ChecksumEquals(Arc.Crypto.FarmHash.Hash64(span)))
        {
            return false;
        }

        header.Id = dataHeader.PacketId;
        owner = owner.Slice(DataHeaderSizeObsolete);
        return true;
    }

    internal static unsafe void CreateDataPacket(ref PacketHeaderObsolete header, PacketIdObsolete packetId, ulong dataId, ReadOnlySpan<byte> data, out ByteArrayPool.MemoryOwner owner)
    {// PacketHeader, DataHeader, Data
        if (data.Length > PacketService.SafeMaxPayloadSize)
        {
            throw new ArgumentOutOfRangeException();
        }

        var arrayOwner = PacketPool.Rent();
        var size = PacketService.HeaderSizeObsolete + PacketService.DataHeaderSizeObsolete + data.Length;
        var span = arrayOwner.ByteArray.AsSpan();

        fixed (byte* pb = span)
        {
            header.Id = PacketIdObsolete.Data;
            header.DataSize = (ushort)(PacketService.DataHeaderSizeObsolete + data.Length);
            *(PacketHeaderObsolete*)pb = header;
        }

        span = span.Slice(PacketService.HeaderSizeObsolete);
        var dataHeader = new DataHeader(dataId, packetId, Arc.Crypto.FarmHash.Hash64(data));
        fixed (byte* pb = span)
        {
            *(DataHeader*)pb = dataHeader;
        }

        span = span.Slice(PacketService.DataHeaderSizeObsolete);
        data.CopyTo(span);

        owner = arrayOwner.ToMemoryOwner(0, size);
    }

    internal static unsafe void CreateDataFollowingPacket(ref PacketHeaderObsolete header, ReadOnlySpan<byte> data, out ByteArrayPool.MemoryOwner owner)
    {// PacketHeader, DataHeader, Data
        if (data.Length > PacketService.SafeMaxPayloadSize)
        {
            throw new ArgumentOutOfRangeException();
        }

        var arrayOwner = PacketPool.Rent();
        var size = PacketService.HeaderSizeObsolete + PacketService.DataFollowingHeaderSizeObsolete + data.Length;
        var span = arrayOwner.ByteArray.AsSpan();

        fixed (byte* pb = span)
        {
            header.Id = PacketIdObsolete.DataFollowing;
            header.DataSize = (ushort)(PacketService.DataFollowingHeaderSizeObsolete + data.Length);
            *(PacketHeaderObsolete*)pb = header;
        }

        span = span.Slice(PacketService.HeaderSizeObsolete);
        var dataHeader = new DataFollowingHeader(Arc.Crypto.FarmHash.Hash64(data));
        fixed (byte* pb = span)
        {
            *(DataFollowingHeader*)pb = dataHeader;
        }

        span = span.Slice(PacketService.DataFollowingHeaderSizeObsolete);
        data.CopyTo(span);

        owner = arrayOwner.ToMemoryOwner(0, size);
    }

    internal static unsafe void CreatePacket<T>(ref PacketHeaderObsolete header, T value, PacketIdObsolete rawPacketId, out ByteArrayPool.MemoryOwner owner)
    {
        var arrayOwner = PacketPool.Rent();
        var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
        var packetHeaderSpan = writer.GetSpan(PacketService.HeaderSizeObsolete);
        writer.Advance(PacketService.HeaderSizeObsolete);

        var written = writer.Written;
        TinyhandSerializer.Serialize(ref writer, value);

        fixed (byte* pb = packetHeaderSpan)
        {
            header.Id = rawPacketId;
            header.DataSize = (ushort)(writer.Written - written);
            *(PacketHeaderObsolete*)pb = header;
        }

        writer.FlushAndGetArray(out var array, out var arrayLength, out var isInitialBuffer);
        if (!isInitialBuffer)
        {
            arrayOwner = new(array);
        }

        owner = arrayOwner.ToMemoryOwner(0, arrayLength);
        writer.Dispose();
    }

    internal static unsafe void CreateAckAndPacket<T>(ref PacketHeaderObsolete header, ulong secondGene, T value, PacketIdObsolete rawPacketId, out ByteArrayPool.MemoryOwner owner)
    {
        var arrayOwner = PacketPool.Rent();
        var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
        var span = writer.GetSpan(PacketService.HeaderSizeObsolete * 2);
        writer.Advance(PacketService.HeaderSizeObsolete * 2);

        var written = writer.Written;
        TinyhandSerializer.Serialize(ref writer, value);

        fixed (byte* pb = span)
        {
            (*(PacketHeaderObsolete*)pb).Engagement = header.Engagement;
            (*(PacketHeaderObsolete*)pb).Id = PacketIdObsolete.Ack;
            (*(PacketHeaderObsolete*)pb).DataSize = 0;
            (*(PacketHeaderObsolete*)pb).Gene = header.Gene;

            header.Id = rawPacketId;
            header.DataSize = (ushort)(writer.Written - written);
            header.Gene = secondGene;
            *(PacketHeaderObsolete*)(pb + PacketService.HeaderSizeObsolete) = header;
        }

        writer.FlushAndGetArray(out var array, out var arrayLength, out var isInitialBuffer);
        if (!isInitialBuffer)
        {
            arrayOwner = new(array);
        }

        owner = arrayOwner.ToMemoryOwner(0, arrayLength);
        writer.Dispose();
    }
}
