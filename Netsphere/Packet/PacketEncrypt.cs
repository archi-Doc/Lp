// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class PacketEncrypt : IPacket
{
    public PacketEncrypt()
    {
    }

    public PacketEncrypt(NodeInformation nodeInformation)
    {
        this.PublicKeyX = nodeInformation.PublicKeyX;
        this.PublicKeyY = nodeInformation.PublicKeyY;
        this.Salt = Random.Crypto.NextULong();
    }

    public bool IsResponse => false;

    public PacketId Id => PacketId.Encrypt;

    [Key(0)]
    public byte[] PublicKeyX { get; set; } = default!;

    [Key(1)]
    public byte[] PublicKeyY { get; set; } = default!;

    [Key(2)]
    public ulong Salt { get; set; }
}
