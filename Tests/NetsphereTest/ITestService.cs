using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetsphereTest;

[NetServiceInterface]
public interface ITestService : INetService
{
    public NetTask Send(int x);
}

[NetServiceInterface]
public interface ITestService2 : INetService
{
    public NetTask Send2(int x);
}

[NetServiceInterface]
public interface ITestService3 : INetService
{
    public NetTask Send3(string x, int y);

    public NetTask<int> Increment3(int x);

    public NetTask<ByteArrayPool.MemoryOwner> SendMemoryOwner(ByteArrayPool.MemoryOwner owner);
}

[NetServiceObject]
public class TestServiceImpl0 : NetServiceBase<CustomServiceContext>, INetService
{
    public void Test()
    {
    }
}

// [NetServiceObject]
public class TestServiceImpl : ITestService
{
    public async NetTask Send(int x)
    {
        return;
    }
}

[NetServiceObject]
public class TestServiceImpl2 : TestServiceImpl, ITestService2
{
    public async NetTask Send2(int x)
    {
        return;
    }
}

[NetServiceObject]
public class ExternalServiceImpl : IExternalService
{
    public ExternalServiceImpl(Terminal terminal)
    {
        Console.WriteLine($"ext ctor {terminal.NetBase.NetsphereOptions.ToString()}");
    }

    public async NetTask<int> IncrementExternal(int x)
    {
        Console.WriteLine($"IncrementExternal {x} -> {x + 1}");
        return x + 1;
    }

    public async NetTask SendExternal(int x)
    {
    }

    public NetTask<NetResult> SendExternal(int x, string y)
    {
        throw new NotImplementedException();
    }
}

public class ParentClass
{
    [NetServiceObject]
    internal class NestedServiceImpl3 : ITestService3
    {
        public async NetTask<int> Increment3(int x)
        {
            Console.WriteLine("Increment3");
            return x + 1;
        }

        public async NetTask Send3(string x, int y)
        {
        }

        public async NetTask<ByteArrayPool.MemoryOwner> SendMemoryOwner(ByteArrayPool.MemoryOwner owner)
        {
            return owner;
        }
    }

    [NetServiceInterface]
    public interface INestedService : INetService
    {
        public NetTask<int> Increment3(int x);
    }
}
