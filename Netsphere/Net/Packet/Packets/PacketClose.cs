// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

[TinyhandObject]
public sealed partial class PacketClose : IPacket
{
    public static PacketType PacketType
        => PacketType.Close;

    public PacketClose()
    {
    }
}
