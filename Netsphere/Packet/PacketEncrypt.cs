// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class PacketEncrypt : IPacket
{
    public bool IsResponse => false;

    public PacketId Id => PacketId.Encrypt;

    [Key(0)]
    public byte[] PublicKey { get; set; } = default!;

    [Key(1)]
    public ulong Hint { get; set; }
}
