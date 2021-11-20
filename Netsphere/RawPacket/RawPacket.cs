// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

public enum RawPacketId : byte
{
    Invalid,
    Ack,
    Ping,
    PingResponse,
    Punch,
    PunchResponse,
    GetNodeInformation,
    Relay,
    Encrypt,
    Data,
}

public interface IRawPacket
{
    public RawPacketId Id { get; }
}

[StructLayout(LayoutKind.Explicit)]
internal partial struct RawPacketHeader
{
    [FieldOffset(0)]
    public byte Engagement;

    [FieldOffset(1)]
    public RawPacketId Id;

    [FieldOffset(2)]
    public ushort DataSize;

    [FieldOffset(8)]
    public ulong Gene;
}
