// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct PacketHeader
{// 20 bytes
    public const int Length = 20;
    public const int MaxContentLengtgh = NetControl.MaxPacketLength - Length;

    public readonly ulong Hash; // 8 bytes
    public readonly ushort Engagement; // 2 bytes
    public readonly PacketType PacketType; // 2 bytes
    public readonly ulong Id; // 8 bytes, Packet id / Connection id
    // Content
}
