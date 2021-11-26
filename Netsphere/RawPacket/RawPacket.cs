// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

public enum RawPacketId : ushort
{
    Invalid,
    Ack,
    Relay,
    Encrypt,
    Data,
    Ping,
    PingResponse,
    Punch,
    PunchResponse,
    GetNode,
    GetNodeResponse,
}

public interface IRawPacket
{
    public RawPacketId Id { get; }
}

[StructLayout(LayoutKind.Explicit)]
internal partial struct RawPacketHeader
{
    [FieldOffset(0)]
    public ushort Engagement;

    [FieldOffset(2)]
    public RawPacketId Id;

    [FieldOffset(4)]
    public ushort DataSize;

    [FieldOffset(8)]
    public ulong Gene;
}
