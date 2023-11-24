// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

public enum PacketType : ushort
{
    // Packet types (0-127)
    Close,
    Ping,
    Punch,
    GetInformation,
    Connect,

    // Packet response types (128-255)
    CloseResponse = 128,
    PingResponse,
    PunchResponse,
    GetInformationResponse,
    ConnectResponse,

    // Gene types (256-511)
    Ack = 256,
    Block,
    RPC,
    Stream,
}
