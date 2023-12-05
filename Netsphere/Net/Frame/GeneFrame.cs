// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct GeneFrame
{// 14 bytes, GeneFrameCode
    public const int Length = 14;
    public const int MaxGeneLength = PacketHeader.MaxFrameLength - Length;
    public const int MaxFirstGeneLength = MaxGeneLength - sizeof(uint) - sizeof(ulong);

    public readonly FrameType FrameType; // 2 bytes
    public readonly uint TransmissionId; // 4 bytes
    public readonly uint GeneSerial; // 4 bytes
    public readonly uint GeneMax; // 4 bytes
}
