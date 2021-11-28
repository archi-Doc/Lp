// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class PacketClose : IRawPacket
{
    public RawPacketId Id => RawPacketId.Close;
}
