// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

public enum PacketType : ushort
{
    Invalid,
    Ack,
    Close,
    Relay,
    Ping,
    PingResponse,
    Punch,
    PunchResponse,
}
