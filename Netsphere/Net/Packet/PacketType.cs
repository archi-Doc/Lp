// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

public enum PacketType : ushort
{
    // Packet types (0-127)
    Connect = 0,
    Ping,
    Punch,
    GetInformation,

    // Packet response types (128-255)
    ConnectResponse = 128,
    PingResponse,
    PunchResponse,
    GetInformationResponse,

    // Gene types (256-383)
    Encrypted = 256,

    // Packet response types (384-512)
    EncryptedResponse = 384,
}
