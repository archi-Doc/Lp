// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.NetServices;

namespace LP.NetServices;

[NetServiceObject]
[NetServiceFilter(typeof(TestOnlyFilter))]
public class BenchmarkServiceImpl : IBenchmarkService
{
    public BenchmarkServiceImpl(ILogger<IBenchmarkService> logger)
    {
        this.logger = logger;
    }

    public async NetTask Report(IBenchmarkService.ReportRecord record)
    {
        this.logger.TryGet()?.Log(record.ToString());
    }

    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    public async NetTask Send(byte[] data)
    {
    }

    private ILogger logger;
}
