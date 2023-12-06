// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

// NetTerminalGene by Nihei.
internal readonly struct GeneFrame
{// 14 bytes, GeneFrameCode
    public const int Length = 14;
    public const int MaxGeneLength = PacketHeader.MaxFrameLength - Length;
    public const int MaxFirstGeneLength = MaxGeneLength - sizeof(uint) - sizeof(ulong);

    public readonly FrameType FrameType; // 2 bytes
    public readonly uint TransmissionId; // 4 bytes
    public readonly uint GenePosition; // 4 bytes
    public readonly uint GeneTotal; // 4 bytes
}

internal readonly struct FirstGeneFrame
{// FirstGeneFrameCode
    public const int Length = 22;
    public const int MaxGeneLength = PacketHeader.MaxFrameLength - Length;

    public readonly FrameType FrameType; // 2 bytes
    public readonly uint TransmissionId; // 4 bytes
    public readonly uint GeneTotal; // 4 bytes
    public readonly uint PrimaryId; // 4 bytes
    public readonly ulong SecondaryId; // 8 bytes
}

internal readonly struct FollowingGeneFrame
{// FollowingGeneFrameCode
    public const int Length = 10;
    public const int MaxGeneLength = PacketHeader.MaxFrameLength - Length;

    public readonly FrameType FrameType; // 2 bytes
    public readonly uint TransmissionId; // 4 bytes
    public readonly uint GenePosition; // 4 bytes
}
