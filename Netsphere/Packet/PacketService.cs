// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

internal class PacketService
{
    static PacketService()
    {
        HeaderSize = Marshal.SizeOf(default(RawPacketHeader));
        // PacketInfo = new PacketInfo[] { new(typeof(PacketPunch), 0, false), };

        var relay = new RawPacketRelay();
        relay.NextEndpoint = new(IPAddress.IPv6Loopback, NetControl.MaxPort);
        RelayPacketSize = Tinyhand.TinyhandSerializer.Serialize(relay).Length;
        SafeMaxPacketSize = NetControl.MaxPayload - HeaderSize - RelayPacketSize - 8;
    }

    public PacketService()
    {
    }

    public static int HeaderSize { get; }

    public static int RelayPacketSize { get; }

    public static int SafeMaxPacketSize { get; }

    // public static PacketInfo[] PacketInfo;

    private const int InitialBufferLength = 2048;

    [ThreadStatic]
    private static byte[]? initialBuffer;

    internal static unsafe byte[] CreatePacket<T>(ref RawPacketHeader header, T value, RawPacketId rawPacketId)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferLength];
        }

        var writer = new Tinyhand.IO.TinyhandWriter(initialBuffer);
        var span = writer.GetSpan(PacketService.HeaderSize);
        writer.Advance(PacketService.HeaderSize);

        var written = writer.Written;
        TinyhandSerializer.Serialize(ref writer, value);

        fixed (byte* pb = span)
        {
            header.Id = rawPacketId;
            header.DataSize = (ushort)(writer.Written - written);
            *(RawPacketHeader*)pb = header;
        }

        return writer.FlushAndGetArray();
    }

    internal static unsafe byte[] CreateAckAndPacket<T>(ref RawPacketHeader header, ulong secondGene, T value, RawPacketId rawPacketId)
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
            (*(RawPacketHeader*)pb).Engagement = header.Engagement;
            (*(RawPacketHeader*)pb).Id = RawPacketId.Ack;
            (*(RawPacketHeader*)pb).DataSize = 0;
            (*(RawPacketHeader*)pb).Gene = header.Gene;

            header.Id = rawPacketId;
            header.DataSize = (ushort)(writer.Written - written);
            header.Gene = secondGene;
            *(RawPacketHeader*)(pb + PacketService.HeaderSize) = header;
        }

        return writer.FlushAndGetArray();
    }
}
