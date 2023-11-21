// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

/// <summary>
/// Packet class requirements.<br/>
/// 1. Inherit IPacket interface.<br/>
/// 2. Has TinyhandObjectAttribute (Tinyhand serializable).<br/>
/// 3. Has unique PacketId.<br/>
/// 4. Length of serialized byte array is less than or equal to <see cref="PacketService.DataPayloadSize"/>.
/// </summary>
public interface IPacket
{
    static abstract PacketType PacketType { get; }
}
