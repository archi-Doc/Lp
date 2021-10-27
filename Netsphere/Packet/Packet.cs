// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LP.Net;

[StructLayout(LayoutKind.Explicit)]
[TinyhandObject]
internal partial struct PacketHeader
{
    [FieldOffset(0)]
    [Key(0)]
    public byte Engagement;

    [FieldOffset(1)]
    [Key(1)]
    public PacketId Id;

    [FieldOffset(2)]
    [Key(2)]
    public ulong Gene;
}

internal enum PacketId : byte
{
    Punch,
    PunchResponse,
}

[TinyhandUnion(0, typeof(PacketPunchResponse))]
internal abstract partial class IPacket
{
    [Key(0)]
    public PacketHeader Header;
}

[TinyhandObject]
internal partial class PacketPunchResponse : IPacket
{
    [Key(1)]
    public IPEndPoint EndPoint { get; set; } = default!;

    [Key(2)]
    public long UtcTicks { get; set; }

    public PacketPunchResponse()
    {
        this.Header.Id = PacketId.PunchResponse;
    }
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
