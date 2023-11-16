// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

/// <summary>
/// Represents a received data.<br/>
/// <see cref="NetResult.Success"/>: <see cref="NetReceivedData.Received"/> is valid, and it's preferable to call Return() method.<br/>
/// Other: <see cref="NetReceivedData.Received"/> is default (empty).
/// </summary>
public record struct NetReceivedData
{
    public NetReceivedData(NetResult result, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner received)
    {
        this.Result = result;
        this.PacketId = packetId;
        this.DataId = dataId;
        this.Received = received;
    }

    public NetReceivedData(NetResult result)
    {
        this.Result = result;
        this.PacketId = PacketId.Invalid;
        this.DataId = 0;
        this.Received = default;
    }

    public void Return() => this.Received.Return();

    public NetResult Result;
    public PacketId PacketId;
    public ulong DataId;
    public ByteArrayPool.MemoryOwner Received;
}
