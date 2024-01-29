// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

public class ReceiveStream : IDisposable
{
    public ReceiveStream(ulong dataId, ByteArrayPool.MemoryOwner toBeShared)
    {
        this.DataId = dataId;
    }

    #region FieldAndProperty

    public ulong DataId { get; }

    #endregion

    public void Dispose()
    {
    }

    public async Task<ByteArrayPool.MemoryOwner> Receive()
    {
        return default;
    }
}
