// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Net;

[TinyhandObject]
internal partial class PacketPunch : IUnmanagedPacket
{
    public PacketPunch()
    {
    }

    public PacketPunch(IPEndPoint? nextEndpoint)
    {
        this.NextEndpoint = nextEndpoint;
        this.UtcTicks = Ticks.GetUtcNow();
    }

    public bool IsManaged => false;

    public UnmanagedPacketId Id => UnmanagedPacketId.Punch;

    [Key(0)]
    public IPEndPoint? NextEndpoint { get; set; }

    [Key(1)]
    public long UtcTicks { get; set; }
}

[TinyhandObject]
internal partial class PacketPunchResponse : IUnmanagedPacket
{
    public bool IsResponse => true;

    public UnmanagedPacketId Id => UnmanagedPacketId.PunchResponse;

    [Key(0)]
    public IPEndPoint Endpoint { get; set; } = default!;

    [Key(1)]
    public long UtcTicks { get; set; }
}
