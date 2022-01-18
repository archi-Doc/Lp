using Netsphere;

namespace NetsphereTest;

public class CustomServiceContext : ServerContext
{
}

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
        Console.Write("Wait -> ");
        await Task.Delay(millisecondsToWait);
        Console.WriteLine($"{millisecondsToWait}");
    }
}

public class TestFilterB : TestFilter
{
    public async ValueTask Invoke(ServerContext context, Func<ServerContext, ValueTask> next)
    {
        await next(context);
    }

}
public class TestFilter : IServiceFilter
{
    public ValueTask Invoke(CallContext context, Func<ServerContext, ValueTask> next)
    {
        throw new NotImplementedException();
    }
}

public class TestFilter2 : IServiceFilter<CallContext>
{
    public async ValueTask Invoke(CallContext context, Func<CallContext, ValueTask> next)
    {
        await next(context);
    }
}
