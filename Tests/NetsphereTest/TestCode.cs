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

[NetServiceObject]
public class CustomService : NetServiceBase<TestServerContext>, ICustomService
{
    public async NetTask Test()
    {
        var serverContext = TestCallContext.Current;
    }
}

public class CustomFilter : IServiceFilter<TestCallContext>
{
    public ValueTask Invoke(TestCallContext context, Func<TestCallContext, ValueTask> next)
    {
        throw new NotImplementedException();
    }
}
