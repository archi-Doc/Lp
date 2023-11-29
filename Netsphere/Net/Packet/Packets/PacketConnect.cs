// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using Netsphere.Block;
using Netsphere.Crypto;
using Netsphere.Server;

namespace Netsphere.Packet;

[TinyhandObject]
internal partial class PacketConnect : IPacket
{
    public static PacketType PacketType => PacketType.Connect;

    public PacketConnect()
    {
    }

    public PacketConnect(ushort engagement, NodePublicKey clientPublicKey)
    {
        this.Engagement = engagement;
        this.ClientPublicKey = clientPublicKey;
        this.ClientSalt = RandomVault.Crypto.NextUInt64();
        this.ClientSalt2 = RandomVault.Crypto.NextUInt64();
    }

    [Key(0)]
    public ushort Engagement { get; set; }

    [Key(1)]
    public NodePublicKey ClientPublicKey { get; set; }

    [Key(2)]
    public ulong ClientSalt { get; set; }

    [Key(3)]
    public ulong ClientSalt2 { get; set; }
}

[TinyhandObject]
internal partial class PacketConnectResponse : IPacket
{
    public static PacketType PacketType => PacketType.ConnectResponse;

    public PacketConnectResponse()
    {
        this.Agreement = ConnectionAgreementBlock.Default;
        this.ServerSalt = RandomVault.Crypto.NextUInt64();
        this.ServerSalt2 = RandomVault.Crypto.NextUInt64();
    }

    public PacketConnectResponse(ServerOptions options)
        : this()
    {
        this.Agreement = new(options);
    }

    [Key(0)]
    public ulong ServerSalt { get; set; }

    [Key(1)]
    public ulong ServerSalt2 { get; set; }

    [Key(2)]
    public ConnectionAgreementBlock Agreement { get; set; }
}
