// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

public class ReceiveStream
{
    internal ReceiveStream(ReceiveTransmission receiveTransmission, ulong dataId, ByteArrayPool.MemoryOwner toBeShared)
    {
        this.receiveTransmission = receiveTransmission;
        this.DataId = dataId;
    }

    #region FieldAndProperty

    public ulong DataId { get; }

    private readonly ReceiveTransmission receiveTransmission;

    #endregion

    public void Abort()
    {
    }

    public async Task<(NetResult Result, int Written)> Receive(Memory<byte> buffer)
    {
        return default;
    }
}
