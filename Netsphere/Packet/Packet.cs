// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LP.Net;

[StructLayout(LayoutKind.Explicit)]
internal struct PacketHeader
{
    [FieldOffset(0)]
    public byte Engagement;

    [FieldOffset(1)]
    public PacketId Id;

    [FieldOffset(2)]
    public ulong Gene;
}

internal enum PacketId : byte
{
    Punch,
}

internal static class PacketHelper
{
    static PacketHelper()
    {
        HeaderSize = Marshal.SizeOf(default(PacketHeader));
    }

    public static int HeaderSize { get; }

    public static unsafe void SetHeader(byte[] buffer, ulong gene, PacketId id)
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
}
