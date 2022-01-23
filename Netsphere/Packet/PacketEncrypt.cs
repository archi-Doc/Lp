// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketEncrypt : IPacket
{
    public PacketId PacketId => PacketId.Encrypt;

    public bool AllowUnencrypted => true;

    public PacketEncrypt()
    {
    }

    public PacketEncrypt(NodeInformation nodeInformation)
    {
        this.NodeInformation = nodeInformation;
        this.Salt = LP.Random.Crypto.NextUInt64();
    }

    [Key(0)]
    public NodeInformation? NodeInformation { get; set; }

    [Key(1)]
    public ulong Salt { get; set; }

    [Key(2)]
    public bool RequestRelay { get; set; }

    [Key(3)]
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
    public NodeAddress? Handover { get; set; }

    [Key(2)]
    public bool CanRelay { get; set; }

    [Key(3)]
    public ushort EnsuredReceiverNumber { get; set; }
}
