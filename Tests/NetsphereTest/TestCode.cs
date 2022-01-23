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

    public NetTask AsyncSync();
}

[NetServiceObject]
[NetServiceFilter(typeof(CustomFilter), Order = 0)]
public class CustomService : ICustomService, ICustomService2
{
    [NetServiceFilter(typeof(CustomFilterAsync), Arguments = new object[] { 1, 2, new string?[] { "te" }, 3 })]
    async NetTask ICustomService.Test()
    {
        var serverContext = TestCallContext.Current;
    }

    [NetServiceFilter(typeof(CustomFilterAsync), Arguments = new object[] { 9, })]

    public async NetTask Test()
    {
        var serverContext = TestCallContext.Current;
    }

    [NetServiceFilter(typeof(CustomFilterAsync), Order = -1)]
    public async NetTask AsyncSync()
    {
    }
}

public class CustomFilterAsync : IServiceFilterAsync
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        await invoker(context);
    }
}

public class CustomFilter : IServiceFilter
{
    public void Invoke(CallContext context, Action<CallContext> invoker)
    {
        if (context is not TestCallContext testContext)
        {
            throw new NetException(NetResult.NoCallContext);
        }

        invoker(context);
    }
    /*public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        if (context is not TestCallContext testContext)
        {
            throw new NetException(NetResult.NoCallContext);
        }

        await invoker(context);
    }*/

    public void SetArguments(object[] args)
    {
    }
}
