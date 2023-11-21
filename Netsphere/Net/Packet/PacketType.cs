// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

public enum PacketType : byte
{
    Invalid,
    Ack,
    Close,
    Relay,
    Ping,
    Punch,

    // Response type
    PingResponse = 128,
    PunchResponse,
}
