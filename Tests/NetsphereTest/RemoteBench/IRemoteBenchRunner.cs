// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace LP.NetServices;

[NetServiceObject]
public class RemoteBenchRunnerImpl : IRemoteBenchRunner
{
    public async NetTask<NetResult> Start(int total, int concurrent)
    {
        return NetResult.Success;
    }
}

[NetServiceInterface]
public partial interface IRemoteBenchRunner : INetService
{
    NetTask<NetResult> Start(int total, int concurrent);
}
