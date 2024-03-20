// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

public enum StreamFrameType : ushort
{
    Complete,
    Cancel,
}

internal readonly struct StreamFrame
{// StreamFrameCode
    public const int Length = 8;

    public readonly FrameType FrameType; // 2 bytes
    public readonly uint TransmissionId; // 4 bytes
    public readonly StreamFrameType StreamFrameType; // 2 bytes
}
