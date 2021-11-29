// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class PacketConnect : IPacket
{
    public PacketConnect()
    {
    }

    public PacketConnect(NodeInformation nodeInformation)
    {
        this.NodeInformation = nodeInformation;
        this.Salt = Random.Crypto.NextULong();
    }

    public PacketId Id => PacketId.Connect;

    [Key(0)]
    public NodeInformation? NodeInformation { get; set; }

    [Key(1)]
    public ulong Salt { get; set; }

    [Key(2)]
    public bool RequestRelay { get; set; }
}

[TinyhandObject]
internal partial class PacketConnectResponse : IPacket
{
    public PacketConnectResponse()
    {
    }

    public PacketId Id => PacketId.ConnectResponse;

    [Key(0)]
    public bool CanRelay { get; set; }
}
