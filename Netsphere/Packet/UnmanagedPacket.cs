// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

public enum UnmanagedPacketId : byte
{
    Invalid,
    Ack,
    Ping,
    Punch,
    PunchResponse,
    GetNodeInformation,
    Relay,
    Encrypt,
    Data,
}

public interface IUnmanagedPacket
{
    public UnmanagedPacketId Id { get; }
}

[StructLayout(LayoutKind.Explicit)]
internal partial struct PacketHeader
{
    [FieldOffset(0)]
    public byte Engagement;

    [FieldOffset(1)]
    public UnmanagedPacketId Id;

    [FieldOffset(2)]
    public ushort DataSize;

    [FieldOffset(8)]
    public ulong Gene;
}
