// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere.Relay;

[TinyhandObject]
public sealed partial class SetRelayPacket : IPacket
{
    public static PacketType PacketType => PacketType.SetRelay;

    public SetRelayPacket()
    {
    }

    [Key(0)]
    public NetEndpoint OuterEndPoint { get; set; }
}

[TinyhandObject]
public sealed partial class SetRelayResponse : IPacket
{
    public static PacketType PacketType => PacketType.SetRelayResponse;

    public SetRelayResponse()
    {
    }

    [Key(0)]
    public RelayResult Result { get; set; }
}
