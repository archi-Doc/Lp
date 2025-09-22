// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.
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
    public RemoteControlServiceImpl(ILogger<RemoteControlServiceImpl> logger, LpUnit lpUnit)
    {
        this.logger = logger;
        this.lpUnit = lpUnit;
    }

    public async NetTask Authenticate(AuthenticationToken token)
    {// NetTask<NetResult> is recommended.
        if (TransmissionContext.Current.ServerConnection.DestinationEndpoint.IsPrivateOrLocalLoopbackAddress() &&
            TransmissionContext.Current.ServerConnection.ValidateAndVerifyWithSalt(token) &&
            token.PublicKey.Equals(this.lpUnit.LpBase.RemotePublicKey))
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
                _ = this.lpUnit.TryTerminate(true);
            });

            return NetResult.Success;
        }

        return NetResult.NotAuthorized;
    }

    private ILogger<RemoteControlServiceImpl> logger;
    private LpUnit lpUnit;
    private AuthenticationToken? token;
}
*/
