﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.
/*
using Netsphere.Crypto;

namespace Lp.NetServices;

[NetServiceInterface]
public interface IRemoteControlService : INetService
{
    public NetTask Authenticate(AuthenticationToken token);

    public NetTask<NetResult> Restart();
}

[NetServiceObject]
internal class RemoteControlServiceImpl : IRemoteControlService
{// Netsphere.Runner -> Container
    // This class is unsafe.
    public RemoteControlServiceImpl(ILogger<RemoteControlServiceImpl> logger, Control control)
    {
        this.logger = logger;
        this.control = control;
    }

    public async NetTask Authenticate(AuthenticationToken token)
    {// NetTask<NetResult> is recommended.
        if (TransmissionContext.Current.ServerConnection.DestinationEndpoint.IsPrivateOrLocalLoopbackAddress() &&
            TransmissionContext.Current.ServerConnection.ValidateAndVerifyWithSalt(token) &&
            token.PublicKey.Equals(this.control.LpBase.RemotePublicKey))
        {
            this.token = token;
            TransmissionContext.Current.Result = NetResult.Success;
            return;
        }

        TransmissionContext.Current.Result = NetResult.NotAuthorized;
    }

    public async NetTask<NetResult> Restart()
    {
        if (this.token == null)
        {
            return NetResult.NotAuthorized;
        }

        if (TransmissionContext.Current.ServerConnection.DestinationEndpoint.IsPrivateOrLocalLoopbackAddress())
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
    private AuthenticationToken? token;
}
*/
