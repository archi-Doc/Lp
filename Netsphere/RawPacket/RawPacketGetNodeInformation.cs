// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

[TinyhandObject]
internal partial class PacketGetNode : IRawPacket
{
    public PacketGetNode()
    {
    }

    public RawPacketId Id => RawPacketId.GetNode;

    [Key(0)]
    public long UtcTicks { get; set; }

    [Key(1)]
    public NodeInformation[]? Nodes { get; set; }
}
