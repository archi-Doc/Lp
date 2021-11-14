// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

public enum PacketId : byte
{
    Invalid,
    Punch,
    PunchResponse,
    GetNodeInformation,
    Relay,
    Encrypt,
    Data,
}

// internal record PacketInfo(Type PacketType, byte PacketId, bool IsResponse);

// [TinyhandUnion((int)PacketId.Punch, typeof(PacketPunch))]
// [TinyhandUnion((int)PacketId.PunchResponse, typeof(PacketPunchResponse))]
public interface IPacket
{
    public bool IsResponse { get; }

    public PacketId Id { get; }
}

[StructLayout(LayoutKind.Explicit)]
// [TinyhandObject]
internal partial struct PacketHeader
{
    [FieldOffset(0)]
    // [Key(0)]
    public byte Engagement;

    [FieldOffset(1)]
    // [Key(1)]
    public PacketId Id;

    [FieldOffset(2)]
    // [Key(2)]
    public ushort DataSize;

    [FieldOffset(8)]
    // [Key(3)]
    public ulong Gene;
}

internal static class PacketExtentions
{
    public static bool IsResponse(this PacketId id) => id switch
    {
        PacketId.PunchResponse => true,
        _ => false,
    };
}
