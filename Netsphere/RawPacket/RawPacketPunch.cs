// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

[TinyhandObject]
internal partial class RawPacketPunch : IRawPacket
{
    public RawPacketPunch()
    {
    }

    public RawPacketPunch(IPEndPoint? nextEndpoint)
    {
        this.NextEndpoint = nextEndpoint;
        this.UtcTicks = Ticks.GetUtcNow();
    }

    public RawPacketId Id => RawPacketId.Punch;

    [Key(0)]
    public IPEndPoint? NextEndpoint { get; set; }

    [Key(1)]
    public long UtcTicks { get; set; }
}

[TinyhandObject]
internal partial class PacketPunchResponse : IRawPacket
{
    public RawPacketId Id => RawPacketId.PunchResponse;

    [Key(0)]
    public IPEndPoint Endpoint { get; set; } = default!;

    [Key(1)]
    public long UtcTicks { get; set; }
}
