using Netsphere;

namespace NetsphereTest;

[NetServiceInterface]
public partial interface IBenchmarkService : INetService
{
    public NetTask Send(byte[] data);

    public NetTask<byte[]?> Pingpong(byte[] data);

    public NetTask Wait(int millisecondsToWait);
}

public partial interface IBenchmarkService
{
}

[NetServiceFilter(typeof(TestFilter), Order = 1)]
[NetServiceFilter(typeof(TestFilterB), Order = 1)]
[NetServiceObject]
public class BenchmarkServiceImpl : IBenchmarkService
{
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

public class TestFilter : IServiceFilterAsync
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        await invoker(context);
    }
}

public class NullFilter : IServiceFilterAsync
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        context.Result = NetResult.NoNetService;
    }
}

public class TestFilter2 : IServiceFilterAsync
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        await next(context);
    }
}
