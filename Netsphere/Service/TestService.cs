// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public interface ITestService : INetService
{
    public Task<int> Increment(int x);

    public Task<NetResult> Send(int x, int y);

    public Task Send2(int x, int y);
}

// [NetServiceObject(true)]
public class TestServiceImpl : ITestService
{
    public TestServiceImpl(Terminal terminal)
    {
    }

    public async Task<int> Increment(int x)
    {
        Console.WriteLine($"Increment: {x + 1}");
        return x + 1;
    }

    public async Task<NetResult> Send(int x, int y)
    {
        Console.WriteLine($"Send: {x}, {y}");
        return default;
    }

    public async Task Send2(int x, int y)
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

    public NetResult Result => this.result;

    private NetResult result;

    public TestServiceFrontend(ClientTerminal clientTerminal)
    {
        this.ClientTerminal = clientTerminal;
    }

    public ClientTerminal ClientTerminal { get; }

    public async Task<int> Increment(int x)
    {
        if (!BlockService.TrySerialize(x, out var owner))
        {
            this.result = NetResult.SerializationError;
            return default;
        }

        var response = await this.ClientTerminal.SendAndReceiveServiceAsync(Id0, owner);
        this.result = response.Result;
        owner.Return();
        if (this.result == NetResult.Success && response.Value.IsEmpty)
        {
            this.result = NetResult.NoNetService;
        }

        if (this.result != NetResult.Success)
        {
            return default;
        }

        if (!TinyhandSerializer.TryDeserialize<(NetResult, int)>(response.Value.Memory, out var result))
        {
            this.result = NetResult.DeserializationError;
            return default;
        }

        this.result = result.Item1;
        response.Value.Return();
        return result.Item2;
    }

    public async Task<NetResult> Send(int x, int y)
    {
        if (!BlockService.TrySerialize((x, y), out var owner))
        {
            this.result = NetResult.SerializationError;
            return this.result;
        }

        var response = await this.ClientTerminal.SendAndReceiveServiceAsync(Id1, owner);
        this.result = response.Result;
        owner.Return();
        if (this.result == NetResult.Success && response.Value.IsEmpty)
        {
            this.result = NetResult.NoNetService;
        }

        if (this.result != NetResult.Success)
        {
            return this.result;
        }

        if (!TinyhandSerializer.TryDeserialize<NetResult>(response.Value.Memory, out var result))
        {
            this.result = NetResult.DeserializationError;
            return this.result;
        }

        this.result = result;
        response.Value.Return();
        return this.result;
    }

    public async Task Send2(int x, int y)
    {
        if (!BlockService.TrySerialize((x, y), out var owner))
        {
            this.result = NetResult.SerializationError;
            return;
        }

        var response = await this.ClientTerminal.SendAndReceiveServiceAsync(Id2, owner);
        this.result = response.Result;
        owner.Return();
        if (this.result == NetResult.Success && response.Value.IsEmpty)
        {
            this.result = NetResult.NoNetService;
        }

        if (this.result != NetResult.Success)
        {
            return;
        }

        if (!TinyhandSerializer.TryDeserialize<NetResult>(response.Value.Memory, out var result))
        {
            this.result = result;
            return;
        }

        this.result = result;
        response.Value.Return();
        return;
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

    public static async Task<ByteArrayPool.MemoryOwner> Identifier3323(object obj, ByteArrayPool.MemoryOwner receive)
    {
        if (!BlockService.TryDeserialize<int>(receive, out var value))
        {
            return default;
        }

        var result = await ((TestServiceBackend)obj).impl.Increment(value);
        BlockService.TrySerialize((NetResult.Success, result), out var send);
        return send;
    }

    public static async Task<ByteArrayPool.MemoryOwner> Identifier3324(object obj, ByteArrayPool.MemoryOwner receive)
    {
        if (!BlockService.TryDeserialize<(int, int)>(receive, out var value))
        {
            return default;
        }

        var result = await ((TestServiceBackend)obj).impl.Send(value.Item1, value.Item2);
        BlockService.TrySerialize(result, out var send);
        return send;
    }

    public static async Task<ByteArrayPool.MemoryOwner> Identifier3325(object obj, ByteArrayPool.MemoryOwner receive)
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
