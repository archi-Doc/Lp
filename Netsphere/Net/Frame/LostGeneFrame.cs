// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct LostGeneFrame
{// LostGeneFrameCode
    public const int Length = 14;
    public const int LengthExcludingFrameType = Length - 2;

    public readonly FrameType FrameType; // 2 bytes
    public readonly uint TransmissionId; // 4 bytes
    public readonly int LostGeneStart; // 4 bytes, the first GeneSerial of the lost gene.
    public readonly int AckGene; // 4 bytes, the first GeneSerial of the received gene.
}
