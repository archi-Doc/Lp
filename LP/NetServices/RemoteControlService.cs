// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using Netsphere;
using NetsphereTest;

namespace LP.NetServices;

[NetServiceObject]
internal class RemoteControlService : IRemoteControlService
{// This class is unsafe.
    public async NetTask RequestAuthorization(Token token)
    {
        var callContext = CallContext.Current;
        // if (callContext.ServerContext.Terminal.Endpoint.Address.)
    }

    public async NetTask Restart()
    {
    }
}
