// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere.Relay;

[TinyhandObject]
public sealed partial class PingRelayPacket : IPacket
{
    public static PacketType PacketType => PacketType.PingRelay;

    public PingRelayPacket()
    {
    }
}

[TinyhandObject]
public sealed partial class PingRelayResponse : IPacket
{
    public static PacketType PacketType => PacketType.PingRelayResponse;

    public PingRelayResponse()
    {
    }

    public PingRelayResponse(long relayPoint, NetEndpoint outerEndPoint)
    {
        this.RelayPoint = relayPoint;
        this.OuterEndPoint = outerEndPoint;
    }

    [Key(0)]
    public long RelayPoint { get; set; }

    [Key(1)]
    public NetEndpoint? OuterEndPoint { get; set; }

    public bool IsOutermost
        => this.OuterEndPoint is null;

    public override string ToString()
    {
        var outerRelay = this.OuterEndPoint is null ? string.Empty : $", OuterRelayAddress: {this.OuterEndPoint}";

        return $"RelayPoint: {this.RelayPoint}{outerRelay}";
    }
}
