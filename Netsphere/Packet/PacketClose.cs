// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketClose : IPacket
{
    public PacketId PacketId => PacketId.Close;
}
