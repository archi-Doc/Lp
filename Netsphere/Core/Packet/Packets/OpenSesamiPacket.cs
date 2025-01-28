// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

[TinyhandObject]
public sealed partial class OpenSesamiPacket : IPacket
{
    public static PacketType PacketType => PacketType.OpenSesami;

    public OpenSesamiPacket()
    {
    }

    public OpenSesamiPacket(NetEndpoint sourceEndpoint)
    {
        this.SourceEndpoint = sourceEndpoint;
    }

    [Key(0)]
    public NetEndpoint SourceEndpoint { get; set; }

    public override string ToString()
        => $"{this.SourceEndpoint}";
}

[TinyhandObject]
public sealed partial class OpenSesamiResponse : IPacket
{
    public static PacketType PacketType => PacketType.OpenSesamiResponse;

    public OpenSesamiResponse()
    {
    }

    public OpenSesamiResponse(NetEndpoint secretEndpoint)
    {
        this.SecretEndpoint = secretEndpoint;
    }

    [Key(0)]
    public NetEndpoint SecretEndpoint { get; set; }

    public override string ToString()
        => $"{this.SecretEndpoint}";
}
