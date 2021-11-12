// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

[TinyhandObject]
internal partial class PacketPunch : IPacket
{
    public PacketPunch()
    {
    }

    public PacketPunch(IPEndPoint? nextEndpoint, long utcTicks)
    {
        this.NextEndpoint = nextEndpoint;
        this.UtcTicks = utcTicks;
    }

    public bool IsResponse => false;

    public PacketId Id => PacketId.Punch;

    [Key(0)]
    public IPEndPoint? NextEndpoint { get; set; }

    [Key(1)]
    public long UtcTicks { get; set; }
}

[TinyhandObject]
internal partial class PacketPunchResponse : IPacket
{
    public bool IsResponse => true;

    public PacketId Id => PacketId.PunchResponse;

    [Key(0)]
    public IPEndPoint Endpoint { get; set; } = default!;

    [Key(1)]
    public long UtcTicks { get; set; }
}
