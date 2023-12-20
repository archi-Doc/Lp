// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

public readonly struct ReceiveStreamResult : IDisposable
{
    public ReceiveStreamResult(NetResult result, ReceiveStream? stream = default)
    {
        this.Result = result;
        this.Stream = stream;
    }

    public readonly NetResult Result;
    public readonly ReceiveStream? Stream;

    public void Dispose()
        => this.Stream?.Dispose();
}
