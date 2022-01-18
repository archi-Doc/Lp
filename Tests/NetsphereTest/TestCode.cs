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

public class CustomFilter : IServiceFilter<TestCallContext>
{
    public ValueTask Invoke(TestCallContext context, Func<TestCallContext, ValueTask> next)
    {
        throw new NotImplementedException();
    }
}
