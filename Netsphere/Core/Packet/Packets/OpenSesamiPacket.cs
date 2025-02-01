// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

[TinyhandObject]
public sealed partial class OpenSesamiPacket : IPacket
{
    public static PacketType PacketType => PacketType.OpenSesami;

    public OpenSesamiPacket(NetAddress sourceAddress)
    {
        this.SourceAddress = sourceAddress;
    }

    public OpenSesamiPacket()
    {
    }

    [Key(0)]
    public NetAddress SourceAddress { get; set; }

    public override string ToString()
        => $"{this.SourceAddress}";
}

[TinyhandObject]
public sealed partial class OpenSesamiResponse : IPacket
{
    public static PacketType PacketType => PacketType.OpenSesamiResponse;

    public OpenSesamiResponse()
    {
    }

    public OpenSesamiResponse(NetAddress secretAddress)
    {
        this.SecretAddress = secretAddress;
    }

    [Key(0)]
    public NetAddress SecretAddress { get; set; }

    public override string ToString()
        => $"{this.SecretAddress}";
}
