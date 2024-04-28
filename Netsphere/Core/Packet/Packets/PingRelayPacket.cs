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

    public PingRelayResponse(long relayPoint, IPEndPoint? outerRelayEndPoint)
    {
        this.RelayPoint = relayPoint;
        this.OuterRelayEndPoint = outerRelayEndPoint;
    }

    [Key(0)]
    public long RelayPoint { get; set; }

    [Key(1)]
    public IPEndPoint? OuterRelayEndPoint { get; set; }

    public bool IsOutermost
        => this.OuterRelayEndPoint is null;

    public override string ToString()
    {
        var outerRelay = this.OuterRelayEndPoint is null ? string.Empty : $", OuterRelayAddress: {this.OuterRelayEndPoint}";

        return $"RelayPoint: {this.RelayPoint}{outerRelay}";
    }
}
