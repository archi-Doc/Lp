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
        relay.NextEndpoint = new(IPAddress.IPv6Loopback, Netsphere.MaxPort);
        RelayPacketSize = Tinyhand.TinyhandSerializer.Serialize(relay).Length;
        SafeMaxPacketSize = Netsphere.MaxPayload - HeaderSize - RelayPacketSize - 8;
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

    internal static unsafe byte[] CreatePacket<T>(ref RawPacketHeader header, T value)
        where T : IRawPacket
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
            header.Id = value.Id;
            header.DataSize = (ushort)(writer.Written - written);
            *(RawPacketHeader*)pb = header;
        }

        return writer.FlushAndGetArray();
    }
}
