// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public enum PacketId : byte
{
    Invalid,
    Ack,
    Close,
    Relay,
    Data,
    DataFollowing,
    Reserve,
    ReserveResponse,
    Rpc,
    Test,
    Encrypt,
    EncryptResponse,
    Punch,
    PunchResponse,
    Ping,
    PingResponse,
    GetNodeInformation,
    GetNodeInformationResponse,
}
