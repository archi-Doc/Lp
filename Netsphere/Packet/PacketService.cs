// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Netsphere;

internal class PacketService
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
    }

    public PacketService()
    {
    }

    public static int HeaderSize { get; }

    public static int DataHeaderSize { get; }

    public static int RelayPacketSize { get; }

    public static int SafeMaxPacketSize { get; }

    // public static PacketInfo[] PacketInfo;

    private const int InitialBufferLength = 2048;

    [ThreadStatic]
    private static byte[]? initialBuffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsManualAck(PacketId id) => id switch
    {
        PacketId.Encrypt => true,
        PacketId.Punch => true,
        PacketId.Ping => true,
        _ => false,
    };

    internal static unsafe byte[] CreatePacket(ref PacketHeader header, PacketId packetId, ulong id, byte[] data)
    {// PacketHeader, DataHeader, Data
        if (data.Length > PacketService.SafeMaxPacketSize)
        {
            throw new ArgumentOutOfRangeException();
        }

        var dataSpan = data.AsSpan();
        var size = PacketService.HeaderSize + PacketService.DataHeaderSize + data.Length;
        var buffer = new byte[size];
        var span = buffer.AsSpan();

        fixed (byte* pb = span)
        {
            header.Id = PacketId.Data;
            header.DataSize = (ushort)(PacketService.DataHeaderSize + data.Length);
            *(PacketHeader*)pb = header;
        }

        span = span.Slice(PacketService.HeaderSize);
        DataHeader dataHeader = default;
        dataHeader.PacketId = packetId;
        dataHeader.Id = id;
        dataHeader.Checksum = Arc.Crypto.FarmHash.Hash64(dataSpan);
        fixed (byte* pb = span)
        {
            *(DataHeader*)pb = dataHeader;
        }

        span = span.Slice(PacketService.DataHeaderSize);
        dataSpan.CopyTo(span);

        return buffer;
    }

    internal static unsafe byte[] CreatePacket<T>(ref PacketHeader header, T value, PacketId rawPacketId)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferLength];
        }

        var writer = new Tinyhand.IO.TinyhandWriter(initialBuffer);
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

        return writer.FlushAndGetArray();
    }

    internal static unsafe byte[] CreateAckAndPacket<T>(ref PacketHeader header, ulong secondGene, T value, PacketId rawPacketId)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferLength];
        }

        var writer = new Tinyhand.IO.TinyhandWriter(initialBuffer);
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

        return writer.FlushAndGetArray();
    }
}
