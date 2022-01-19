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
public class BenchmarkServiceImpl : NetServiceBase, IBenchmarkService
{
    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    public async NetTask Send(byte[] data)
    {
    }

    public async NetTask Wait(int millisecondsToWait)
    {
        if (CallContext.Current is not TestCallContext context)
        {
            throw new NetException(NetResult.NoCallContext);
        }

        Console.Write("Wait -> ");
        await Task.Delay(millisecondsToWait);
        Console.WriteLine($"{millisecondsToWait}");

    }
}

public class TestFilterB : TestFilter
{
    public async Task Invoke(ServerContext context, Func<ServerContext, Task> next)
    {
        await next(context);
    }

}
public class TestFilter : IServiceFilter
{
    public Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        throw new NotImplementedException();
    }
}

public class TestFilter2 : IServiceFilter<CallContext>
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        await next(context);
    }
}
