// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

#pragma warning disable CS0649

internal readonly struct ProtectedPacket
{// Protected = Salt + Encrypted + Checksum
    public const int Length = 10;

    public readonly ulong Checksum;
    public readonly FrameType FrameType;
    // Frame
}
