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

public class TestServiceClient : ITestService
{
    public TestServiceClient(ClientTerminal clientTerminal)
    {
        this.ClientTerminal = clientTerminal;
    }

    public ClientTerminal ClientTerminal { get; }

    public async Task<int> Increment(int x)
    {
        if (!BlockService.TrySerialize(x, out var owner))
        {
            return default;
        }

        var response = await this.ClientTerminal.SendAndReceiveServiceAsync(0, owner);
        owner.Return();
        if (response.Result != NetResult.Success)
        {
            return default;
        }

        TinyhandSerializer.TryDeserialize<int>(response.Value.Memory, out var result);
        response.Value.Return();
        return result;
    }

    public async Task<NetResult> Send(int x, int y)
    {
        if (!BlockService.TrySerialize((x, y), out var owner))
        {
            return NetResult.SerializationError;
        }

        /*var result = await this.ClientTerminal.SendServiceAsync(0, owner);
        owner.Return();
        return result;*/

        var response = await this.ClientTerminal.SendAndReceiveServiceAsync(0, owner);
        owner.Return();
        if (response.Result != NetResult.Success)
        {
            return response.Result;
        }

        TinyhandSerializer.TryDeserialize<NetResult>(response.Value.Memory, out var result);
        response.Value.Return();
        return result;
    }

    public async Task Send2(int x, int y)
    {
        if (!BlockService.TrySerialize((x, y), out var owner))
        {
            return;
        }

        var result = await this.ClientTerminal.SendServiceAsync(0, owner);
        owner.Return();
    }
}

internal class TestServiceServer
{
    public TestServiceServer(IServiceProvider? serviceProvider)
    {
        var impl = serviceProvider?.GetService(typeof(TestServiceImpl)) as TestServiceImpl;
        if (impl == null)
        {
            // impl = new TestServiceImpl();
            throw new InvalidOperationException();
        }

        this.impl = impl;
    }

    public static ByteArrayPool.MemoryOwner Identifier3323(object obj, ByteArrayPool.MemoryOwner owner)
    {
        if (!BlockService.TryDeserialize<int>(owner, out var value))
        {
            return ByteArrayPool.MemoryOwner.Empty;
        }

        var r = ((TestServiceServer)obj).impl.Increment(value);
        BlockService.TrySerialize(r.Result, out var owner2);
        return owner;
    }

    public static NetResult Identifier3324(object obj, ByteArrayPool.MemoryOwner receive, out ByteArrayPool.MemoryOwner send)
    {
        if (!BlockService.TryDeserialize<(int, int)>(receive, out var value))
        {
            send = default;
            return NetResult.DeserializationError;
        }

        var r = ((TestServiceServer)obj).impl.Send(value.Item1, value.Item2);
        BlockService.TrySerialize(r.Result, out send);
        return NetResult.Success;
    }

    public static NetService.ServiceInfo GetServiceInfo()
    {
        var serviceInfo = new NetService.ServiceInfo(0, static x => new TestServiceServer(x));
        serviceInfo.ServiceMethods.TryAdd(0, new NetService.ServiceMethod(0, Identifier3324));
        return serviceInfo;
    }

    private TestServiceImpl impl;
}
