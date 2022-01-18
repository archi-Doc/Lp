using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetsphereTest;

public class TestServerContext : ServerContext
{
}

public class TestCallContext : CallContext<TestServerContext>
{
    public static new TestCallContext Current => (TestCallContext)CallContext.Current;

    public TestCallContext()
    {
    }
}

[NetServiceInterface]
public interface ICustomService : INetService
{
    public NetTask Test();
}

[NetServiceInterface]
public interface ICustomService2 : INetService
{
    public NetTask Test();
}

[NetServiceObject]
[NetServiceFilter(typeof(CustomFilter), Order = 0)]
public class CustomService : NetServiceBase<TestServerContext>, ICustomService, ICustomService2
{
    [NetServiceFilter(typeof(CustomFilter2))]
    async NetTask ICustomService.Test()
    {
        var serverContext = TestCallContext.Current;
    }

    [NetServiceFilter(typeof(CustomFilter), Order = 0)]
    public async NetTask Test()
    {
        var serverContext = TestCallContext.Current;
    }
}

public class CustomFilter : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        await next(context);
    }
}

public class CustomFilter2 : IServiceFilter<TestCallContext>
{
    public async Task Invoke(TestCallContext context, Func<TestCallContext, Task> next)
    {
        await next(context);
    }
}
