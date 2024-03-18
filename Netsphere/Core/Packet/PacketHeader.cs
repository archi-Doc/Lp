// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct PacketHeader
{// 16 bytes, PacketHeaderCode
    public const int Length = 16;
    public const int MaxPayloadLength = NetConstants.MaxPacketLength - Length;
    public const int MaxFrameLength = NetConstants.MaxPacketLength - Length - 16; // PKCS7 padding

    public readonly uint HashSalt; // 4 bytes, Hash / Salt
    public readonly ushort Engagement; // 2 bytes
    public readonly PacketType PacketType; // 2 bytes
    public readonly ulong Id; // 8 bytes, Packet id / Connection id
}
