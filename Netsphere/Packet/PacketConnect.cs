// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketConnect : IPacket
{
    public PacketId Id => PacketId.Connect;

    public bool AllowUnencrypted => true;

    public bool ManualAck => true;

    public PacketConnect()
    {
    }

    public PacketConnect(NodeInformation nodeInformation)
    {
        this.NodeInformation = nodeInformation;
        this.Salt = LP.Random.Crypto.NextULong();
    }


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
    public PacketId Id => PacketId.ConnectResponse;

    public bool AllowUnencrypted => true;

    public bool ManualAck => true;

    public PacketConnectResponse()
    {
    }

    [Key(0)]
    public NodeAddress? HandOver { get; set; }

    [Key(1)]
    public bool CanRelay { get; set; }
}
