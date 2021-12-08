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

/// <summary>
/// Packet class requirements.<br/>
/// 1. Inherit IPacket interface.<br/>
/// 2. Has TinyhandObjectAttribute (Tinyhand serializable).<br/>
/// 3. Has unique PacketId.<br/>
/// 4. Length of serialized byte array is less than or equal to <see cref="PacketService.DataPacketSize"/>.
/// </summary>
public interface IPacket : IBlock
{
    public PacketId PacketId { get; }

    uint IBlock.BlockId => (uint)this.PacketId;

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
