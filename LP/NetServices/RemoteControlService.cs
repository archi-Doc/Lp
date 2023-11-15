// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere;
using Netsphere.Crypto;

namespace LP.NetServices;

[NetServiceInterface]
public interface RemoteControlService : INetService
{
    public NetTask RequestAuthorization(Token token);

    public NetTask<NetResult> Restart();
}

[NetServiceObject]
internal class RemoteControlServiceImpl : RemoteControlService
{// LPRunner -> Container
    // This class is unsafe.
    public RemoteControlServiceImpl(ILogger<RemoteControlServiceImpl> logger, Control control)
    {
        this.logger = logger;
        this.control = control;
    }

    public async NetTask RequestAuthorization(Token token)
    {// NetTask<NetResult> is recommended.
        if (CallContext.Current.ServerContext.Terminal.Node.Address.IsPrivateOrLocalLoopbackAddress() &&
            token.ValidateAndVerifyWithoutSalt(this.control.LPBase.RemotePublicKey))
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
        if (callContext.ServerContext.Terminal.Node.Address.IsPrivateOrLocalLoopbackAddress())
        {// Restart
            this.logger.TryGet()?.Log("RemoteControlService.Restart()");

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                _ = this.control.TryTerminate(true);
            });

            return NetResult.Success;
        }

        return NetResult.NotAuthorized;
    }

    private ILogger<RemoteControlServiceImpl> logger;
    private Control control;
    private Token? token;
}
