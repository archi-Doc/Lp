// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Netsphere;

public enum PacketId : byte
{
    Invalid,
    Ack,
    Close,
    Relay,
    Data,
    Reserve,
    RPC,
    Encrypt,
    EncryptResponse,
    Ping,
    PingResponse,
    Punch,
    PunchResponse,
    GetNode,
    GetNodeResponse,
}

public interface IPacket
{
    public PacketId Id { get; }

    public bool AllowUnencrypted => false;
}

[StructLayout(LayoutKind.Explicit)]
internal struct PacketHeader
{
    [FieldOffset(0)]
    public ushort Engagement;

    [FieldOffset(2)]
    public byte Cage;

    [FieldOffset(3)]
    public PacketId Id;

    [FieldOffset(4)]
    public ushort DataSize;

    [FieldOffset(8)]
    public ulong Gene;
}
