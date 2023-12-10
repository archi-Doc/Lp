// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere;

[TinyhandObject]
internal partial class PacketEncryptObsolete : IPacketObsolete
{
    public PacketIdObsolete PacketId => PacketIdObsolete.Encrypt;

    public bool AllowUnencrypted => true;

    public PacketEncryptObsolete()
    {
    }

    public PacketEncryptObsolete(NodePublicKey publicKey)
    {
        this.PublicKey = publicKey;
        this.Salt = RandomVault.Crypto.NextUInt64();
        this.SaltA = RandomVault.Crypto.NextUInt64();
    }

    [Key(0)]
    public NodePublicKey PublicKey { get; set; }

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
internal partial class PacketEncryptResponseObsolete : IPacketObsolete
{
    public PacketIdObsolete PacketId => PacketIdObsolete.EncryptResponse;

    public bool AllowUnencrypted => true;

    // public bool ManualAck => true;

    public PacketEncryptResponseObsolete()
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
