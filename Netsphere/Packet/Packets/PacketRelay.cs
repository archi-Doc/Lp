// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketRelayObsolete : IPacketObsolete
{
    public const int EndpointAliveInSeconds = 10;

    public PacketIdObsolete PacketId => PacketIdObsolete.Relay;

    [Key(0)]
    public IPEndPoint? NextEndpoint { get; set; }

    [Key(1)]
    public bool IsDestination { get; set; }
}
