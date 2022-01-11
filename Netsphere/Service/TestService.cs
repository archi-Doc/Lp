// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Design;

public interface ITestService : INetService
{
    public NetTask<int> Increment(int x);

    public NetTask<NetResult> Send(int x, int y);

    public NetTask Send2(int x, int y);
}

// [NetServiceObject(true)]
public class TestServiceImpl : ITestService
{
    public TestServiceImpl(Terminal terminal)
    {
    }

    public async NetTask<int> Increment(int x)
    {
        Console.WriteLine($"Increment: {x + 1}");
        return x + 1;
    }

    public async NetTask<NetResult> Send(int x, int y)
    {
        Console.WriteLine($"Send: {x}, {y}");
        return default;
    }

    public async NetTask Send2(int x, int y)
    {
        Console.WriteLine($"Send2: {x}, {y}");
    }
}

public class TestServiceFrontend : ITestService
{
    public static readonly uint ServiceId = 0x6F;
    public static readonly ulong Id0 = 0x6F000000DEul;
    public static readonly ulong Id1 = 0x6F000000DFul;
    public static readonly ulong Id2 = 0x6F000000E0ul;

    public TestServiceFrontend(ClientTerminal clientTerminal)
    {
        this.ClientTerminal = clientTerminal;
    }

    public ClientTerminal ClientTerminal { get; }

    public NetTask<int> Increment(int x)
    {
        return new NetTask<int>(Core());

        async Task<ServiceResponse<int>> Core()
        {
            if (!BlockService.TrySerialize(x, out var owner))
            {
                return new(default, NetResult.SerializationError);
            }

            var response = await this.ClientTerminal.SendAndReceiveServiceAsync(Id0, owner);
            owner.Return();
            if (response.Result == NetResult.Success && response.Value.IsEmpty)
            {
                return new(default, NetResult.NoNetService);
            }
            else if (response.Result != NetResult.Success)
            {
                return new(default, response.Result);
            }

            if (!TinyhandSerializer.TryDeserialize<int>(response.Value.Memory, out var result))
            {
                return new(default, NetResult.DeserializationError);
            }

            response.Value.Return();
            return new(result);
        }
    }

    public NetTask<NetResult> Send(int x, int y)
    {
        return new NetTask<NetResult>(Core());

        async Task<ServiceResponse<NetResult>> Core()
        {
            if (!BlockService.TrySerialize((x, y), out var owner))
            {
                return new(NetResult.SerializationError, NetResult.SerializationError);
            }

            var response = await this.ClientTerminal.SendAndReceiveServiceAsync(Id1, owner);
            owner.Return();
            if (response.Result == NetResult.Success && response.Value.IsEmpty)
            {
                return new(NetResult.NoNetService, NetResult.NoNetService);
            }
            else if (response.Result != NetResult.Success)
            {
                return new(response.Result, response.Result);
            }

            if (!TinyhandSerializer.TryDeserialize<NetResult>(response.Value.Memory, out var result))
            {
                return new(NetResult.DeserializationError, NetResult.DeserializationError);
            }

            response.Value.Return();
            return new(result, result);
        }
    }

    public NetTask Send2(int x, int y)
    {
        return new NetTask(Core());

        async Task<ServiceResponse> Core()
        {
            if (!BlockService.TrySerialize((x, y), out var owner))
            {
                return new(NetResult.SerializationError);
            }

            var response = await this.ClientTerminal.SendAndReceiveServiceAsync(Id2, owner);
            owner.Return();
            if (response.Result == NetResult.Success && response.Value.IsEmpty)
            {
                return new(NetResult.NoNetService);
            }
            else if (response.Result != NetResult.Success)
            {
                return new(response.Result);
            }

            if (!TinyhandSerializer.TryDeserialize<NetResult>(response.Value.Memory, out var result))
            {
                return new(NetResult.DeserializationError);
            }

            response.Value.Return();
            return default;
        }
    }
}

public class TestServiceBackend
{
    public TestServiceBackend(IServiceProvider? serviceProvider)
    {
        var impl = serviceProvider?.GetService(typeof(TestServiceImpl)) as TestServiceImpl;
        if (impl == null)
        {
            // impl = new TestServiceImpl();
            throw new InvalidOperationException($"Could not create an instance of net service {typeof(TestServiceImpl).ToString()}.");
        }

        this.impl = impl;
    }

    public static async NetTask<ByteArrayPool.MemoryOwner> Identifier3323(object obj, ByteArrayPool.MemoryOwner receive)
    {
        if (!BlockService.TryDeserialize<int>(receive, out var value))
        {
            return default;
        }

        var result = await ((TestServiceBackend)obj).impl.Increment(value);
        BlockService.TrySerialize(result, out var send);
        return send;
    }

    public static async NetTask<ByteArrayPool.MemoryOwner> Identifier3324(object obj, ByteArrayPool.MemoryOwner receive)
    {
        if (!BlockService.TryDeserialize<(int, int)>(receive, out var value))
        {
            return default;
        }

        var result = await ((ITestService)((TestServiceBackend)obj).impl).Send(value.Item1, value.Item2);
        BlockService.TrySerialize(result, out var send);
        return send;
    }

    public static async NetTask<ByteArrayPool.MemoryOwner> Identifier3325(object obj, ByteArrayPool.MemoryOwner receive)
    {
        if (!BlockService.TryDeserialize<(int, int)>(receive, out var value))
        {
            return default;
        }

        await ((TestServiceBackend)obj).impl.Send2(value.Item1, value.Item2);
        var result = NetResult.Success;
        BlockService.TrySerialize(result, out var send);
        return send;
    }

    public static NetService.ServiceInfo CreateServiceInfo()
    {
        var serviceInfo = new NetService.ServiceInfo(TestServiceFrontend.ServiceId, static x => new TestServiceBackend(x));
        serviceInfo.AddMethod(new NetService.ServiceMethod(TestServiceFrontend.Id0, Identifier3323));
        serviceInfo.AddMethod(new NetService.ServiceMethod(TestServiceFrontend.Id1, Identifier3324));
        serviceInfo.AddMethod(new NetService.ServiceMethod(TestServiceFrontend.Id2, Identifier3325));
        return serviceInfo;
    }

    private TestServiceImpl impl;
}
