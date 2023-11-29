// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct GeneFrame
{// 18 bytes, BlockFrameCode
    public const int MaxBlockLength = PacketHeader.MaxFrameLength - 14;

    public readonly FrameType FrameType; // 2 bytes
    public readonly uint TransmissionId; // 4 bytes
    public readonly uint GeneSerial; // 4 bytes
    public readonly uint GeneMax; // 4 bytes
}
