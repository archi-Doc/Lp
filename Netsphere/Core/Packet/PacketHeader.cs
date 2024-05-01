// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct PacketHeader
{// 18 bytes, PacketHeaderCode, CreatePacketCode
    public const int Length = 18;
    public const int MaxPayloadLength = NetConstants.MaxPacketLength - NetConstants.RelayLength - Length;
    public const int MaxFrameLength = NetConstants.MaxPacketLength - NetConstants.RelayLength - Length - ProtectedPacket.Length; // - 16 (PKCS7 padding requires 16 bytes, but trimming should just fit within the upper limit, so it's probably fine)

    public readonly ushort SourceRelayId; // 2 bytes
    public readonly ushort DestinationRelayId; // 2 bytes
    public readonly uint HashSalt; // 4 bytes, Hash / Salt
    public readonly PacketType PacketType; // 2 bytes
    public readonly ulong Id; // 8 bytes, Packet id / Connection id
    // Frame
}
