// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;

namespace Netsphere;

[TinyhandObject]
internal partial class PacketEncrypt : IPacket
{
    public PacketId PacketId => PacketId.Encrypt;

    public bool AllowUnencrypted => true;

    public PacketEncrypt()
    {
    }

    public PacketEncrypt(NetNode node)
    {
        this.Node = node;
        this.Salt = RandomVault.Crypto.NextUInt64();
        this.SaltA = RandomVault.Crypto.NextUInt64();
    }

    [Key(0)]
    public NetNode Node { get; set; }

    [Key(1)]
    public ulong Salt { get; set; }

    [Key(2)]
    public ulong SaltA { get; set; }

    [Key(3)]
    public bool RequestRelay { get; set; }

    [Key(4)]
    public ushort RequestReceiverNumber { get; set; }
}

[TinyhandObject]
internal partial class PacketEncryptResponse : IPacket
{
    public PacketId PacketId => PacketId.EncryptResponse;

    public bool AllowUnencrypted => true;

    // public bool ManualAck => true;

    public PacketEncryptResponse()
    {
    }

    [Key(0)]
    public ulong Salt2 { get; set; }

    [Key(1)]
    public ulong SaltA2 { get; set; }

    // [Key(2)]
    // public NodeAddress? Handover { get; set; }

    [Key(3)]
    public bool CanRelay { get; set; }

    [Key(4)]
    public ushort EnsuredReceiverNumber { get; set; }
}
