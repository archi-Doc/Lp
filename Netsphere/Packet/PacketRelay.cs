// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketRelay : IPacket
{
    public const int EndpointAliveInSeconds = 10;

    public PacketId Id => PacketId.Relay;

    [Key(0)]
    public IPEndPoint? NextEndpoint { get; set; }

    [Key(1)]
    public bool IsDestination { get; set; }
}
