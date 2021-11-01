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
        HeaderSize = Marshal.SizeOf(default(PacketHeader));
        // PacketInfo = new PacketInfo[] { new(typeof(PacketPunch), 0, false), };
    }

    public PacketService()
    {
    }

    public static int HeaderSize { get; }

    // public static PacketInfo[] PacketInfo;

    private const int InitialBufferLength = 2048;

    [ThreadStatic]
    private static byte[]? initialBuffer;

    public unsafe void SetHeader(byte[] buffer, ulong gene, PacketId id)
    {
        if (buffer.Length < HeaderSize)
        {
            throw new ArgumentException();
        }

        var header = default(PacketHeader);
        header.Gene = gene;
        header.Id = id;
        fixed (byte* pb = buffer)
        {
            *(PacketHeader*)pb = header;
        }
    }

    internal static unsafe byte[] CreatePacket<T>(ref PacketHeader header, T value)
        where T : IPacket
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
            *(PacketHeader*)pb = header;
        }

        return writer.FlushAndGetArray();
    }
}
