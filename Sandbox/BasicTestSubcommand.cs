// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using Arc.Unit;
using Netsphere;
using Netsphere.Block;
using Netsphere.Net;
using Netsphere.Packet;
using Netsphere.Responder;
using Netsphere.Server;
using SimpleCommandLine;

namespace Sandbox;

public class CustomConnectionContext : ConnectionContext
{
    public CustomConnectionContext(ServerConnection serverConnection)
        : base(serverConnection)
    {
    }

    /*public override async Task InvokeStream(StreamContext streamContext)
    {
        var buffer = new byte[100_000];
        var hash = new FarmHash();
        hash.HashInitialize();
        long total = 0;

        while (true)
        {
            var r = await streamContext.Receive(buffer);
            if (r.Result == NetResult.Success ||
                r.Result == NetResult.Completed)
            {
                // Console.WriteLine($"recv {r.Written}");
                hash.HashUpdate(buffer.AsMemory(0, r.Written).Span);
                total += r.Written;
            }
            else
            {
                break;
            }

            if (r.Result == NetResult.Completed)
            {
                var h = BitConverter.ToUInt64(hash.HashFinal());
                Debug.Assert(h == streamContext.DataId);

                streamContext.SendAndForget(h);
                break;
            }
        }
    }*/

    public override ConnectionAgreementBlock RequestAgreement(ConnectionAgreementBlock agreement)
    {// Accept the request
        agreement.Update(this.ServerConnection.Agreement);
        return agreement;
    }
}

[SimpleCommand("basic")]
public class BasicTestSubcommand : ISimpleCommandAsync<BasicTestOptions>
{
    public BasicTestSubcommand(ILogger<BasicTestSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(BasicTestOptions options, string[] args)
    {
        var nodeString = options.Node;
        if (string.IsNullOrEmpty(nodeString))
        {
            nodeString = "alternative";
        }

        if (!NetAddress.TryParse(this.logger, nodeString, out var netAddress))
        {
            return;
        }

        this.NetControl.Responders.Register(MemoryResponder.Instance);
        this.NetControl.Responders.Register(TestBlockResponder.Instance);
        this.NetControl.Responders.Register(TestStreamResponder.Instance);
        this.NetControl.Services.Register<TestService>();

        var sw = Stopwatch.StartNew();
        var netTerminal = this.NetControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var p = new PacketPing("test56789");
        var result = await packetTerminal.SendAndReceive<PacketPing, PacketPingResponse>(netAddress, p);

        Console.WriteLine($"{sw.ElapsedMilliseconds} ms, {result.ToString()}");
        sw.Restart();

        Console.WriteLine($"{sw.ElapsedMilliseconds} ms, {result.ToString()}");

        var netNode = await netTerminal.UnsafeGetNetNodeAsync(netAddress);
        if (netNode is null)
        {
            return;
        }

        this.NetControl.NetBase.ServerConnectionContext = connection => new CustomConnectionContext(connection);
        // this.NetControl.NetBase.ServerOptions = this.NetControl.NetBase.ServerOptions with { MaxStreamLength = 100_000_000, };

        // netTerminal.PacketTerminal.MaxResendCount = 0;
        // netTerminal.SetDeliveryFailureRatioForTest(0.03);
        // netTerminal.SetReceiveTransmissionGapForTest(1);
        using (var connection = await netTerminal.TryConnect(netNode))
        {
            if (connection is not null)
            {
                var success = 0;

                var agreement = TinyhandSerializer.Clone(connection.Agreement);
                agreement.MaxStreamLength = 100_000_000;
                var agreementResult = await connection.RequestAgreement(agreement);

                var service = connection.GetService<TestService>();
                var pingpong = await service.Pingpong([1, 2, 3,]);
                /*var response = await service.ReceiveData("test", 123_000).ResponseAsync;
                if (response.Value is not null)
                {
                    await this.ProcessReceiveStream(response.Value);
                }*/

                var stream = await service.SendData(123_000);
                if (stream is not null)
                {
                    await this.ProcessSendStream(stream, 123_000);
                }

                await Console.Out.WriteLineAsync("SendData2");
                var stream2 = await service.SendData2(123_000);
                if (stream2 is not null)
                {
                    await this.ProcessSendStream(stream2, 123_000);
                }

                /*for (var i = 0; i < 20; i++)
                {
                    var testBlock = TestBlock.Create(10);
                    var r = await connection.SendAndReceive<TestBlock, TestBlock>(testBlock);
                    if (testBlock.Equals(r.Value))
                    {
                        success++;
                    }
                }*/

                /*for (var i = 0; i < 20_000; i += 1_000)
                {
                    Console.WriteLine($"TestBlock: {i}");
                    var testBlock = TestBlock.Create(i);
                    var r = await connection.SendAndReceive<TestBlock, TestBlock>(testBlock);
                    if (testBlock.Equals(r.Value))
                    {
                        success++;
                    }
                }

                var tasks = new List<Task>();
                var count = 0;
                var array = new byte[] { 0, 1, 2, };
                for (var i = 0; i < 20; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var r = await connection.SendAndReceive<Memory<byte>, Memory<byte>>(array);
                        if (r.Value.Span.SequenceEqual(array))
                        {
                            Interlocked.Increment(ref count);
                            Interlocked.Increment(ref success);
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                Console.WriteLine(count);*/

                /*await this.TestStream2(connection, 1_000);
                await this.TestStream2(connection, 10_000);
                await this.TestStream2(connection, 100_000);
                await this.TestStream2(connection, 1_000_000);*/
                // await this.TestStream2(connection, 10_000_000);

                // await this.TestStream3(connection, 1_000);
                // await this.TestStream3(connection, 10_000);
                // await this.TestStream3(connection, 100_000);
                // await this.TestStream3(connection, 1_000_000);

                /*using (var result2 = await connection.SendAndReceiveStream(p2))
                {
                    if (result2.Stream is { } stream)
                    {
                        var result3 = await stream.Receive();

                        result3.Return();
                    }
                }*/

                /*using (var stream = await connection.CreateStream(1000))
                {
                    if (stream is not null)
                    {
                        var result2 = await stream.SendAsync([]);
                        stream.Dispose();
                    }
                }*/

                /*var transmission = await connection.CreateTransmission();
                if (transmission is not null)
                {
                    transmission.SendAndForget();
                }*/

                // connection.Close();
                // var r = await connection.SendAndReceiveAsync<PacketPing, PacketPingResponse>(netAddress, p);

                Console.WriteLine($"Success: {success}, Send: {connection.SendCount}, Resend: {connection.ResendCount}");
            }
        }

        /*using (var connection = await netTerminal.TryConnect(netNode))
        {// Reuse connection
            if (connection is not null)
            {
                var service = connection.GetService<TestService>();
                var result2 = await service.Pingpong([0, 1, 2]);
                Console.WriteLine(result2?.Length.ToString());
            }
        }*/
    }

    private async Task TestStream(ClientConnection connection, int size)
    {
        var buffer = new byte[size];
        RandomVault.Pseudo.NextBytes(buffer);
        var hash = FarmHash.Hash64(buffer);

        var r = await connection.SendStream(size, hash);
        Debug.Assert(r.Result == NetResult.Success);
        if (r.Stream is not null)
        {
            var result2 = await r.Stream.Send(buffer);
            await r.Stream.Complete();
        }
    }

    private async Task TestStream2(ClientConnection connection, int size)
    {
        var buffer = new byte[size];
        RandomVault.Pseudo.NextBytes(buffer);
        var hash = FarmHash.Hash64(buffer);

        var r = await connection.SendStreamAndReceive<ulong>(size, hash);
        Debug.Assert(r.Result == NetResult.Success);
        if (r.Stream is not null)
        {
            var r2 = await r.Stream.Send(buffer);
            var r3 = await r.Stream.CompleteAndReceive();
            Debug.Assert(r3.Value == hash);
        }
    }

    private async Task TestStream3(ClientConnection connection, int size)
    {
        var (_, stream) = await connection.SendAndReceiveStream(size, TestStreamResponder.Instance.DataId);
        if (stream is not null)
        {
            var buffer = new byte[100_000];
            var hash = new FarmHash();
            hash.HashInitialize();
            long total = 0;

            while (true)
            {
                var r = await stream.Receive(buffer);
                await Console.Out.WriteLineAsync($"{r.Result.ToString()} {r.Written}");
                if (r.Result == NetResult.Success ||
                    r.Result == NetResult.Completed)
                {
                    hash.HashUpdate(buffer.AsMemory(0, r.Written).Span);
                    total += r.Written;
                }
                else
                {
                    break;
                }

                if (r.Result == NetResult.Completed)
                {
                    var h = BitConverter.ToUInt64(hash.HashFinal());
                    Debug.Assert(h == stream.DataId);
                    break;
                }
            }
        }
    }

    private async Task ProcessReceiveStream(ReceiveStream stream)
    {
        if (stream is not null)
        {
            var buffer = new byte[100_000];
            var hash = new FarmHash();
            hash.HashInitialize();
            long total = 0;

            while (true)
            {
                var r = await stream.Receive(buffer);
                await Console.Out.WriteLineAsync($"{r.Result.ToString()} {r.Written}");
                if (r.Result == NetResult.Success ||
                    r.Result == NetResult.Completed)
                {
                    hash.HashUpdate(buffer.AsMemory(0, r.Written).Span);
                    total += r.Written;
                }
                else
                {
                    break;
                }

                if (r.Result == NetResult.Completed)
                {
                    var h = BitConverter.ToUInt64(hash.HashFinal());
                    Debug.Assert(h == stream.DataId);
                    break;
                }
            }
        }
    }

    private async Task ProcessSendStream(SendStreamAndReceive<ulong> stream, int size)
    {
        var buffer = new byte[size];
        RandomVault.Pseudo.NextBytes(buffer);
        var hash = FarmHash.Hash64(buffer);

        var r2 = await stream.Send(buffer);
        var r3 = await stream.CompleteAndReceive();
        Debug.Assert(r3.Value == hash);
    }

    private async Task ProcessSendStream(SendStream stream, int size)
    {
        var buffer = new byte[size];
        RandomVault.Pseudo.NextBytes(buffer);
        var hash = FarmHash.Hash64(buffer);

        var r2 = await stream.Send(buffer);
        Console.WriteLine(r2.ToString());
        var r3 = await stream.Complete();
        Console.WriteLine(r3.ToString());
    }

    public NetControl NetControl { get; set; }

    private ILogger<BasicTestSubcommand> logger;
}

public record BasicTestOptions
{
    [SimpleOption("node", Description = "Node address", Required = false)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
