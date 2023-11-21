// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

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
