// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct AckFrame
{// 16 bytes, AckFrameCode
    public const int MaxAck = (PacketHeader.MaxFrameLength - 2) / 8;

    public readonly FrameType FrameType; // 2 bytes
    public readonly uint TransmissionId; // 4 bytes
    public readonly uint GenePosition; // 4 bytes
}
