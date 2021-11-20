// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class RawPacketRelay : IRawPacket
{
    public const int EndpointAliveInSeconds = 10;

    public RawPacketId Id => RawPacketId.Relay;

    [Key(0)]
    public IPEndPoint? NextEndpoint { get; set; }

    [Key(1)]
    public bool IsDestination { get; set; }
}
