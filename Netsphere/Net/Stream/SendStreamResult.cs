// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

public readonly struct SendStreamResult : IDisposable
{
    public SendStreamResult(NetResult result, SendStream? stream = default)
    {
        this.Result = result;
        this.Stream = stream;
    }

    public readonly NetResult Result;
    public readonly SendStream? Stream;

    public void Dispose()
        => this.Stream?.Dispose();
}
