// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere.Packet;

[TinyhandObject]
public partial class GetInformationPacket : IPacket
{
    public static PacketType PacketType => PacketType.GetInformation;

    public GetInformationPacket()
    {
    }
}

[TinyhandObject]
public partial class GetInformationPacketResponse : IPacket
{
    public static PacketType PacketType => PacketType.GetInformationResponse;

    public GetInformationPacketResponse()
    {
    }

    public GetInformationPacketResponse(EncryptionPublicKey2 publicKey)
    {
        this.PublicKey = publicKey;
    }

    [Key(0)]
    public EncryptionPublicKey2 PublicKey { get; private set; }
}
