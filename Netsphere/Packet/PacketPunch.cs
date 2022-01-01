// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Netsphere;

[TinyhandObject]
public partial class PacketPunch : IPacket
{
    public PacketId PacketId => PacketId.Punch;

    public bool AllowUnencrypted => true;

    public PacketPunch()
    {
    }

    public PacketPunch(IPEndPoint? nextEndpoint)
    {
        this.NextEndpoint = nextEndpoint;
        this.UtcMics = Mics.GetUtcNow();
    }

    [Key(0)]
    public IPEndPoint? NextEndpoint { get; set; }

    [Key(1)]
    public long UtcMics { get; set; }
}

[TinyhandObject]
public partial class PacketPunchResponse : IPacket
{
    public PacketId PacketId => PacketId.PunchResponse;

    public bool AllowUnencrypted => true;

    [Key(0)]
    public IPEndPoint Endpoint { get; set; } = default!;

    [Key(1)]
    public long UtcMics { get; set; }

    public override string ToString() => $"{this.Endpoint}";
}
