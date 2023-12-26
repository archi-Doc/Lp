// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;
using Netsphere.Packet;
using Netsphere.Server;

namespace Netsphere.Responder;

public class PingPacketResponder : SyncResponder<PacketPing, PacketPingResponse>
{
    public static readonly INetResponder Instance = new PingPacketResponder();

    public override ulong DataId => BlockService.GetPacketId<PacketPing, PacketPingResponse>();

    public override PacketPingResponse? RespondSync(PacketPing value)
    {
        return new(NetAddress.Alternative, "Alt");
    }
}
