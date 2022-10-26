// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Netsphere;

namespace LP.NetServices;

[NetServiceObject]
internal class RemoteControlService : IRemoteControlService
{// LPRunner -> Container
    // This class is unsafe.
    public RemoteControlService(ILogger<RemoteControlService> logger, Control control)
    {
        this.logger = logger;
        this.control = control;
    }

    public async NetTask RequestAuthorization(Token token)
    {// NetTask<NetResult> is recommended.
        if (CallContext.Current.ServerContext.Terminal.NodeAddress.IsPrivateOrLocalLoopbackAddress() && token.ValidateAndVerify(this.control.LPBase.RemotePublicKey))
        {
            this.token = token;
            CallContext.Current.Result = NetResult.Success;
            return;
        }

        CallContext.Current.Result = NetResult.NotAuthorized;
    }

    public async NetTask<NetResult> Restart()
    {
        if (this.token == null)
        {
            return NetResult.NotAuthorized;
        }

        var callContext = CallContext.Current;
        if (callContext.ServerContext.Terminal.NodeAddress.IsPrivateOrLocalLoopbackAddress())
        {// Restart
            this.logger.TryGet()?.Log("RemoteControlService.Restart()");

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                this.control.Terminate(false); // tempcode
            });

            return NetResult.Success;
        }

        return NetResult.NotAuthorized;
    }

    private ILogger<RemoteControlService> logger;
    private Control control;
    private Token? token;
}
