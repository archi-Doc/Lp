// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
public partial class PacketPunchObsolete : IPacketObsolete
{
    public PacketIdObsolete PacketId => PacketIdObsolete.Punch;

    public bool AllowUnencrypted => true;

    public PacketPunchObsolete()
    {
    }

    public PacketPunchObsolete(IPEndPoint? nextEndpoint)
    {
        this.UtcMics = Mics.GetUtcNow();
        this.NextEndpoint = nextEndpoint;
    }

    [Key(0)]
    public long UtcMics { get; set; }

    [Key(1)]
    public bool Relay { get; set; } // Relay this packet to the next endpoint (NextEndpoint must be a valid value).

    [Key(2)]
    public IPEndPoint? NextEndpoint { get; set; }
}

[TinyhandObject]
public partial class PacketPunchResponseObsolete : IPacketObsolete
{
    public PacketIdObsolete PacketId => PacketIdObsolete.PunchResponse;

    public bool AllowUnencrypted => true;

    [Key(0)]
    public long UtcMics { get; set; }

    [Key(1)]
    public IPEndPoint Endpoint { get; set; } = default!;

    public override string ToString() => $"{this.Endpoint}";
}
