// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LPRunner;
using Netsphere;

namespace LP.NetServices;

[NetServiceObject]
internal class RemoteControlService : IRemoteControlService
{// Remote -> LPRunner
    public RemoteControlService(Runner runner)
    {
        this.runner = runner;
    }

    public async NetTask RequestAuthorization(Token token)
    {
    }

    public async NetTask<NetResult> Acknowledge()
    {
        if (!this.IsAuthorized)
        {
            return NetResult.NotAuthorized;
        }

        return NetResult.Success;
    }

    public async NetTask<NetResult> Restart()
    {
        if (!this.IsAuthorized)
        {
            return NetResult.NotAuthorized;
        }

        return NetResult.Success;
    }

    public bool IsAuthorized => this.token != null;

    private Runner runner;
    private Token? token;
}
