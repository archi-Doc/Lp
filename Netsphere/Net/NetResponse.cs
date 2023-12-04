// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere;

/// <summary>
/// Represents a received data.<br/>
/// <see cref="NetResult.Success"/>: <see cref="NetResponse.Received"/> is valid, and it's preferable to call Return() method.<br/>
/// Other: <see cref="NetResponse.Received"/> is default (empty).
/// </summary>
public record struct NetResponse
{
    public NetResponse(NetResult result, PacketType packetId, ulong dataId, ByteArrayPool.MemoryOwner received)
    {
        this.Result = result;
        this.PacketType = packetId;
        this.DataId = dataId;
        this.Received = received;
    }

    public NetResponse(NetResult result)
    {
        this.Result = result;
        this.PacketType = default;
        this.DataId = 0;
        this.Received = default;
    }

    public bool IsSuccess => this.Result == NetResult.Success;

    public void Return() => this.Received.Return();

    public NetResult Result;
    public PacketType PacketType;
    public ulong DataId;
    public ByteArrayPool.MemoryOwner Received;
}
