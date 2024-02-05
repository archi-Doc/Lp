using System.Diagnostics;
using Netsphere.Server;

namespace LP.NetServices;

[NetServiceFilter<TestFilter>(Order = 1)]
[NetServiceFilter<TestFilterB>(Order = 1)]
[NetServiceObject]
public class BenchmarkServiceImpl : IBenchmarkService
{
    public BenchmarkServiceImpl(RemoteBenchBroker remoteBenchBroker)
    {
        this.remoteBenchBroker = remoteBenchBroker;
    }

    public async NetTask<NetResult> Register()
    {
        return NetResult.NoNetService;
    }

    public async NetTask<NetResult> Start(int total, int concurrent)
    {
        this.remoteBenchBroker.Start(total, concurrent);
        return NetResult.Success;
    }

    public async NetTask Report(IBenchmarkService.ReportRecord record)
    {
        // await Console.Out.WriteLineAsync(record.ToString());
    }

    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    public async NetTask Send(byte[] data)
    {
    }

    public NetTask<SendStreamAndReceive<ulong>?> GetHash(long maxLength)
    {
        return default;
    }

    private RemoteBenchBroker remoteBenchBroker;
}

public class TestFilterB : TestFilter
{
    public new async Task Invoke(TransmissionContext context, Func<TransmissionContext, Task> next)
    {
        await next(context);
    }

    public TestFilterB(NetControl aa)
    {
    }
}

public class TestFilter : IServiceFilter
{
    public async Task Invoke(TransmissionContext context, Func<TransmissionContext, Task> invoker)
    {
        await invoker(context);
    }
}

public class NullFilter : IServiceFilter
{
    public async Task Invoke(TransmissionContext context, Func<TransmissionContext, Task> next)
    {
        context.Result = NetResult.NoNetService;
    }
}

public class TestFilter2 : IServiceFilter
{
    public async Task Invoke(TransmissionContext context, Func<TransmissionContext, Task> next)
    {
        await next(context);
    }
}
