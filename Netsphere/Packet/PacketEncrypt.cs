// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class PacketEncrypt : IUnmanagedPacket
{
    public PacketEncrypt()
    {
    }

    public PacketEncrypt(NodeInformation nodeInformation)
    {
        this.NodeInformation = nodeInformation;
        this.Salt = Random.Crypto.NextULong();
    }

    public bool IsManaged => false;

    public UnmanagedPacketId Id => UnmanagedPacketId.Encrypt;

    [Key(0)]
    public NodeInformation? NodeInformation { get; set; }

    [Key(1)]
    public ulong Salt { get; set; }
}
