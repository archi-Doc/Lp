// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.Server;

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
        this.remoteBenchBroker.Register(TransmissionContext.Current.Connection.Node);
        return NetResult.Success;
    }

    public async NetTask<NetResult> Start(int total, int concurrent)
    {
        return NetResult.NoNetService;
    }

    public async NetTask Report(IBenchmarkService.ReportRecord record)
    {
        this.remoteBenchBroker.Report(TransmissionContext.Current.Connection.Node, record);
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
