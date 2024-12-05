// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Netsphere.Relay;

#pragma warning disable CS0649

[StructLayout(LayoutKind.Explicit)]
public readonly struct RelayHeader
{// 32 bytes, RelayHeaderCode
    public const int RelayIdLength = 4; // SourceRelayId/DestinationRelayId
    public const int Length = 32;

    public RelayHeader(uint salt, byte paddingLength, NetAddress netAddress)
    {
        this.Salt = salt;
        this.PaddingLength = paddingLength;
        this.NetAddress = netAddress;
    }

    /*public RelayHeader(uint salt, byte paddingLength, NetEndpoint endpoint)
    {
        this.Salt = salt;
        this.PaddingLength = paddingLength;
        this.NetAddress = new(endpoint);
    }*/

    [FieldOffset(0)]
    public readonly uint Zero; // 4 bytes
    [FieldOffset(4)]
    public readonly uint Salt; // 3 bytes
    [FieldOffset(7)]
    public readonly byte PaddingLength; // 1 bytes
    [FieldOffset(8)]
    public readonly NetAddress NetAddress; // 24 bytes
}
