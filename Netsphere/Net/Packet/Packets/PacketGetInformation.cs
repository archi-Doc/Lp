// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere.Packet;

[TinyhandObject]
public partial class PacketGetInformation : IPacket
{
    public static PacketType PacketType => PacketType.GetInformation;

    public PacketGetInformation()
    {
    }
}

[TinyhandObject]
public partial class PacketGetInformationResponse : IPacket
{
    public static PacketType PacketType => PacketType.GetInformationResponse;

    public PacketGetInformationResponse()
    {
    }

    public PacketGetInformationResponse(NodePublicKey publicKey)
    {
        this.PublicKey = publicKey;
    }

    [Key(0)]
    public NodePublicKey PublicKey { get; private set; }
}
