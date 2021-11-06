// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class PacketRelay : IPacket
{
    public bool IsResponse => false;

    public PacketId Id => PacketId.Relay;

    [Key(0)]
    public IPEndPoint? NextEndpoint { get; set; }
}
