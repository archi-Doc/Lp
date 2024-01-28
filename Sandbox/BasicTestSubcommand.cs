// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Arc.Unit;
using Netsphere;
using Netsphere.Block;
using Netsphere.Packet;
using SimpleCommandLine;

namespace Sandbox;

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

        this.NetControl.NetResponder.Register(Netsphere.Responder.MemoryResponder.Instance);
        this.NetControl.NetResponder.Register(Netsphere.Responder.TestBlockResponder.Instance);

        var sw = Stopwatch.StartNew();
        var netTerminal = this.NetControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var p = new PacketPing("test56789");
        var result = await packetTerminal.SendAndReceiveAsync<PacketPing, PacketPingResponse>(netAddress, p);

        Console.WriteLine($"{sw.ElapsedMilliseconds} ms, {result.ToString()}");
        sw.Restart();

        Console.WriteLine($"{sw.ElapsedMilliseconds} ms, {result.ToString()}");

        var netNode = await netTerminal.UnsafeGetNetNodeAsync(netAddress);
        if (netNode is null)
        {
            return;
        }

        this.NetControl.NetBase.ServerOptions = this.NetControl.NetBase.ServerOptions with { MaxStreamLength = 4_000_000, };

        // netTerminal.PacketTerminal.MaxResendCount = 0;
        // netTerminal.SetDeliveryFailureRatio(0.2);
        using (var connection = await netTerminal.TryConnect(netNode))
        {
            if (connection is not null)
            {
                var success = 0;

                /*for (var i = 0; i < 20; i++)
                {
                    var testBlock = TestBlock.Create(10);
                    var r = await connection.SendAndReceive<TestBlock, TestBlock>(testBlock);
                    if (testBlock.Equals(r.Value))
                    {
                        success++;
                    }
                }

                for (var i = 0; i < 20_000; i += 1_000)
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

                var (_, stream) = await connection.SendStream(1000);
                if (stream is not null)
                {
                    var result2 = await stream.Send(new byte[123]);
                    await stream.Complete();
                }

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

    public NetControl NetControl { get; set; }

    private ILogger<BasicTestSubcommand> logger;
}

public record BasicTestOptions
{
    [SimpleOption("node", Description = "Node address", Required = false)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
