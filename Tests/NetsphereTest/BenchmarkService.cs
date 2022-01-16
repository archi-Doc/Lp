using Netsphere;

namespace NetsphereTest;

public class CustomServiceContext : ServiceContext
{
}

[NetServiceInterface]
public interface IBenchmarkService : INetService
{
    public NetTask Send(byte[] data);

    public NetTask<byte[]?> Pingpong(byte[] data);

    public NetTask Wait(int millisecondsToWait);
}

[NetServiceFilter(typeof(TestFilter), Order = 1)]
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
    public async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        await next(context);
    }

}
public class TestFilter : IServiceFilter
{
    public async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        await next(context);
    }
}

public class TestFilter2 : IServiceFilter<CustomServiceContext>
{
    public async ValueTask Invoke(CustomServiceContext context, Func<CustomServiceContext, ValueTask> next)
    {
        await next(context);
    }
}
