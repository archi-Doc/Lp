// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class RawPacketEncrypt : IRawPacket
{
    public RawPacketEncrypt()
    {
    }

    public RawPacketEncrypt(NodeInformation nodeInformation)
    {
        this.NodeInformation = nodeInformation;
        this.Salt = Random.Crypto.NextULong();
    }

    public RawPacketId Id => RawPacketId.Encrypt;

    [Key(0)]
    public NodeInformation? NodeInformation { get; set; }

    [Key(1)]
    public ulong Salt { get; set; }
}
