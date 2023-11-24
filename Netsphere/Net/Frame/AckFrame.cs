// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct AckFrame
{// 16 bytes, AckFrameCode
    public const int MaxAck = (PacketHeader.MaxFrameLengtgh - 2) / 8;

    public readonly FrameType FrameType; // 2 bytes
    public readonly uint TransmissionSerial; // 4 bytes
    public readonly uint GeneSerial; // 4 bytes
}
