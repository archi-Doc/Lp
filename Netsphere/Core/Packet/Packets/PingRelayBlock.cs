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

    public PingRelayResponse(long relayPoint, NetEndpoint outerRelayAddress)
    {
        this.RelayPoint = relayPoint;
        this.OuterRelayAddress = outerRelayAddress;
    }

    [Key(0)]
    public long RelayPoint { get; private set; }

    [Key(1)]
    public NetEndpoint OuterRelayAddress { get; private set; }
}
