// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[TinyhandObject]
public sealed partial class PingRelayBlock
{
    public PingRelayBlock()
    {
    }
}

[TinyhandObject]
public sealed partial class PingRelayResponse
{
    public PingRelayResponse()
    {
    }

    public PingRelayResponse(ushort outerRelayId, NetAddress outerRelayAddress, long relayPoint)
    {
    }

    [Key(0)]
    public ushort OuterRelayId { get; private set; }

    [Key(1)]
    public NetAddress OuterRelayAddress { get; private set; }

    [Key(2)]
    public long RelayPoint { get; private set; }
}
