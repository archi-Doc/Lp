// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

public enum PacketId : byte
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

public interface IPacket
{
    public bool IsManaged => true;

    public PacketId Id { get; }
}

[StructLayout(LayoutKind.Explicit)]
internal partial struct PacketHeader
{
    [FieldOffset(0)]
    public byte Engagement;

    [FieldOffset(1)]
    public PacketId Id;

    [FieldOffset(2)]
    public ushort DataSize;

    [FieldOffset(8)]
    public ulong Gene;
}
