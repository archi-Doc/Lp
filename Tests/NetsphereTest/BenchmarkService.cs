using Netsphere;

namespace NetsphereTest;

[NetServiceInterface]
public interface IBenchmarkService : INetService
{
    public NetTask Send(byte[] data);

    public NetTask<byte[]?> Pingpong(byte[] data);
}

[TestFilter]
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
}

public class TestFilter : NetServiceFilterAttribute
{
    public override async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        await next(context);
    }
}
