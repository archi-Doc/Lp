﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Net;
using Arc.Unit;
using LP.T3CS;
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

        this.NetControl.RegisterResponder(Netsphere.Responder.MemoryResponder.Instance);
        this.NetControl.RegisterResponder(Netsphere.Responder.TestBlockResponder.Instance);

        var sw = Stopwatch.StartNew();
        var netTerminal = this.NetControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var p = new PacketPing("test56789");
        var result = await packetTerminal.SendAndReceiveAsync<PacketPing, PacketPingResponse>(netAddress, p);

        Console.WriteLine($"{sw.ElapsedMilliseconds} ms, {result.ToString()}");
        sw.Restart();

        /*for (var i = 0; i < 10; i++)
        {
            p = new PacketPing("test56789");
            result = await packetTerminal.SendAndReceiveAsync<PacketPing, PacketPingResponse>(netAddress, p);
        }*/

        Console.WriteLine($"{sw.ElapsedMilliseconds} ms, {result.ToString()}");

        var netNode = await netTerminal.UnsafeGetNetNodeAsync(netAddress);
        if (netNode is null)
        {
            return;
        }

        netTerminal.PacketTerminal.MaxResendCount = 0; // tempcode
        using (var connection = await netTerminal.TryConnect(netNode))
        {
            if (connection is not null)
            {
                // var service = connection.GetService<TestService>();
                // var result2 = await service.Pingpong([0, 1, 2]);

                // Send Block*Stream, Receive Non*Block*Stream
                // Send(), SendAndReceive(), SendAndReceiveStream(), SendStream(), SendStreamAndReceive()
                /*var p2 = new PacketPing();
                var response = await connection.SendAndReceive<PacketPing, PacketPingResponse>(p2);
                if (response.Value is not null)
                {
                    Console.WriteLine(response.Value.ToString());
                }*/

                await Console.Out.WriteLineAsync();

                for (var i = 0; i < 16_000; i += 1_000)
                {
                    var testBlock = TestBlock.Create(i);
                    var r = await connection.SendAndReceive<TestBlock, TestBlock>(testBlock);
                    Debug.Assert(testBlock.Equals(r.Value));
                }

                var tasks = new List<Task>();
                var count = 0;
                var array = new byte[] { 0, 1, 2, };
                for (var i = 0; i < 10; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var r = await connection.SendAndReceive<Memory<byte>, Memory<byte>>(array);
                        if (r.Value.Span.SequenceEqual(array))
                        {
                            Interlocked.Increment(ref count);
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                Console.WriteLine(count);

                /*using (var stream = await connection.SendStream(1000))
                {
                    if (stream is not null)
                    {
                        var result2 = await stream.Send([]);
                        stream.Dispose();
                    }
                }*/

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
            }
        }

        using (var connection = await netTerminal.TryConnect(netNode))
        {// Reuse connection
            if (connection is not null)
            {
                var service = connection.GetService<TestService>();
                var result2 = await service.Pingpong([0, 1, 2]);
                Console.WriteLine(result2?.Length.ToString());
            }
        }

        // await Task.Delay(1000000000);

        /*if (!NetAddress.TryParse(this.logger, nodeString, out var node))
        {
            return;
        }

        this.logger.TryGet()?.Log($"SendData: {node.ToString()}");
        this.logger.TryGet()?.Log($"{Stopwatch.Frequency}");

        // var nodeInformation = NodeInformation.Alternative;
        using (var terminal = this.NetControl.TerminalObsolete.TryCreate(node))
        {
            if (terminal is null)
            {
                return;
            }

            // terminal.SetMaximumResponseTime(1_000_000);
            var t = await terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(new PacketPunch());
            this.logger.TryGet()?.Log($"{t.ToString()}");
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
