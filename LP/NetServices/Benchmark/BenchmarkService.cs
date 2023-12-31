// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace LP.NetServices;

[NetServiceObject]
[NetServiceFilter<TestOnlyFilter>]
internal class BenchmarkServiceImpl : IBenchmarkService
{
    public BenchmarkServiceImpl(ILogger<IBenchmarkService> logger, RemoteBenchBroker remoteBenchBroker)
    {
        this.logger = logger;
        this.remoteBenchBroker = remoteBenchBroker;
    }

    public async NetTask<NetResult> Register()
    {
        this.remoteBenchBroker.Register(LPCallContext.Current.ServerContext.Terminal.Node);
        return NetResult.Success;
    }

    public async NetTask<NetResult> Start(int total, int concurrent)
    {
        return NetResult.NoNetService;
    }

    public async NetTask Report(IBenchmarkService.ReportRecord record)
    {
        this.remoteBenchBroker.Report(LPCallContext.Current.ServerContext.Terminal.Node, record);
    }

    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    public async NetTask Send(byte[] data)
    {
    }

    private ILogger logger;
    private RemoteBenchBroker remoteBenchBroker;
}
