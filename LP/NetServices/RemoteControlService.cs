// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Netsphere;

namespace LP.NetServices;

[NetServiceObject]
internal class RemoteControlService : IRemoteControlService
{// LPRunner -> Container
    // This class is unsafe and is limited to be accessed from loopback addresses.
    public RemoteControlService(Control control)
    {
        this.control = control;
    }

    public async NetTask RequestAuthorization(Token token)
    {// NetTask<NetResult> is recommended.
        var callContext = CallContext.Current;
        callContext.Result = NetResult.NotAuthorized;
    }

    public async NetTask<NetResult> Restart()
    {
        if (this.token == null)
        {
            return NetResult.NotAuthorized;
        }

        var callContext = CallContext.Current;
        if (callContext.ServerContext.Terminal.NodeAddress.IsLocalLoopbackAddress())
        {// Restart
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                this.control.Terminate(false);
            });

            return NetResult.Success;
        }

        return NetResult.NotAuthorized;
    }

    private Control control;
    private Token? token;
}
