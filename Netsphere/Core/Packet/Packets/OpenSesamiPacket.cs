// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

[TinyhandObject]
public sealed partial class OpenSesamiPacket : IPacket
{
    public static PacketType PacketType => PacketType.OpenSesami;

    public OpenSesamiPacket(NetNode sourceEndpoint)
    {
        this.SourceEndpoint = sourceEndpoint;
    }

    private OpenSesamiPacket()
    {
        this.SourceEndpoint = default!;
    }

    [Key(0)]
    public NetNode SourceEndpoint { get; set; }

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
