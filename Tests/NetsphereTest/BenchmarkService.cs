using NetsphereTest;
using Tinyhand;

namespace LP.NetServices;

[NetServiceInterface]
public partial interface IBenchmarkService : INetService
{
    public NetTask Send(byte[] data);

    public NetTask<byte[]?> Pingpong(byte[] data);

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial record ReportRecord
    {
        public int SuccessCount { get; init; }

        public int FailureCount { get; init; }

        public int Concurrent { get; init; }

        public long ElapsedMilliseconds { get; init; }

        public int CountPerSecond { get; init; }

        public int AverageLatency { get; init; }
    }

    public NetTask Report(ReportRecord record);
}

[NetServiceFilter(typeof(TestFilter), Order = 1)]
[NetServiceFilter(typeof(TestFilterB), Order = 1)]
[NetServiceObject]
public class BenchmarkServiceImpl : IBenchmarkService
{
    public BenchmarkServiceImpl()
    {
    }

    public async NetTask Report(IBenchmarkService.ReportRecord record)
    {
        await Console.Out.WriteLineAsync(record.ToString());
    }

    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    public async NetTask Send(byte[] data)
    {
    }

    // [NetServiceFilter(typeof(NullFilter))]
    public async NetTask Wait(int millisecondsToWait)
    {
        if (CallContext.Current is not TestCallContext context)
        {
            throw new NetException(NetResult.NoCallContext);
        }

        Console.Write("Wait -> ");
        await Task.Delay(millisecondsToWait);
        Console.WriteLine($"{millisecondsToWait}");

        context.Result = NetResult.NoEncryptedConnection;
    }
}

public class TestFilterB : TestFilter
{
    public async Task Invoke(ServerContext context, Func<ServerContext, Task> next)
    {
        await next(context);
    }

    public TestFilterB(NetControl aa)
    {
    }
}

public class TestFilter : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        await invoker(context);
    }
}

public class NullFilter : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        context.Result = NetResult.NoNetService;
    }
}

public class TestFilter2 : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        await next(context);
    }
}
