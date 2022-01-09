using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetsphereTest;

[NetServiceInterface]
public interface ITestService : INetService
{
    public Task Send(int x);
}

[NetServiceInterface]
public interface ITestService2 : INetService
{
    public Task Send2(int x);
}

[NetServiceInterface]
public interface ITestService3 : INetService
{
    public Task Send3(string x, int y);

    public Task<int> Increment3(int x);
}

[NetServiceObject]
public class TestServiceImpl0 : INetService
{
}

// [NetServiceObject]
public class TestServiceImpl : ITestService
{
    public async Task Send(int x)
    {
        return;
    }
}

[NetServiceObject]
public class TestServiceImpl2 : TestServiceImpl, ITestService2
{
    public async Task Send2(int x)
    {
        return;
    }
}

[NetServiceObject]
public class ExternalServiceImpl : IExternalService
{
    public Task<int> IncrementExternal(int x)
    {
        throw new NotImplementedException();
    }

    public async Task SendExternal(int x)
    {
    }

    public Task<NetResult> SendExternal(int x, string y)
    {
        throw new NotImplementedException();
    }
}

public class ParentClass
{
    [NetServiceObject]
    internal class NestedServiceImpl3 : ITestService3
    {
        public async Task<int> Increment3(int x)
        {
            return x + 1;
        }

        public async Task Send3(string x, int y)
        {
        }
    }

    [NetServiceInterface]
    public interface INestedService : INetService
    {
        public Task<int> Increment3(int x);
    }
}
