// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere.Crypto;
using Netsphere.Packet;
using Netsphere.Version;

namespace Netsphere.Relay;

[TinyhandObject]
public sealed partial class UpdateVersionPacket : IPacket
{
    public static PacketType PacketType => PacketType.UpdateVersion;

    public UpdateVersionPacket()
    {
    }

    [Key(0)]
    public CertificateToken<VersionInfo>? Token { get; set; }
}

[TinyhandObject]
public sealed partial class UpdateVersionResponse : IPacket
{
    public static PacketType PacketType => PacketType.UpdateVersionResponse;

    public UpdateVersionResponse()
    {
    }

    public UpdateVersionResponse(NetResult result)
    {
        this.Result = result;
    }

    [Key(0)]
    public NetResult Result { get; set; }
}
