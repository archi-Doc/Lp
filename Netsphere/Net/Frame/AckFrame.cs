// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct AckFrame
{// AckFrameCode
    public const int Length = 2;
    public const int Margin = 32;
    public const int RamaSize = 4;
    public const int BlockSize = 14; // + 8 x NumberOfPairs

    public readonly FrameType FrameType; // 2 bytes

    // public readonly ushort NumberOfRamaTransmissions;
    // public readonly ushort NumberOfBlockTransmissions;

    // x NumberOfRamaTransmissions : Rama
    // public readonly uint TransmissionId;

    // x NumberOfBlockTransmissions : Block/Stream
    // public readonly uint TransmissionId;
    // public readonly int SuccessiveReceivedPosition;
    // public readonly int ReceiveCapacity;
    // public readonly ushort NumberOfPairs; // 2 bytes
    //   public readonly int StartGene; // 4 bytes
    //   public readonly int EndGene; // 4 bytes
}
