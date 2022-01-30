// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP.Subcommands;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace NetsphereTest;

[SimpleCommand("netbench")]
public class NetbenchSubcommand : ISimpleCommandAsync<NetbenchOptions>
{
    public NetbenchSubcommand(NetControl netControl)
    {
        this.NetControl = netControl;
    }

    public async Task Run(NetbenchOptions options, string[] args)
    {
        if (!SubcommandService.TryParseNodeAddress(options.Node, out var node))
        {
            return;
        }

        Logger.Priority.Information($"Netbench: {node.ToString()}");

        // var nodeInformation = NodeInformation.Alternative;
        using (var terminal = this.NetControl.Terminal.Create(node))
        {
            var p = new PacketPunch(null);
            var sw = Stopwatch.StartNew();
            var t = await terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            Logger.Priority.Information($"t: {t}");
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms");
            sw.Restart();

            for (var i = 0; i < 10; i++)
            {
                t = await terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            }

            Logger.Priority.Information($"t: {t}");
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms");

            // await this.BenchLargeData(terminal);
            // await this.PingpongSmallData(terminal);

            // var service = terminal.GetService<IBenchmarkService>();

            /*await service.Wait(200);
            await service.Wait(200);
            await service.Wait(200);*/

            // var tt = await service.Wait(100).ResponseAsync;
            // Console.WriteLine(tt.ToString());

            /*var w1 = service.Wait(200);
            var w2 = service.Wait(200);
            var w3 = service.Wait(200);
            w1.ResponseAsync.Wait();
            w2.ResponseAsync.Wait();
            w3.ResponseAsync.Wait();*/
        }

        // await this.PingpongSmallData2(node);
        // await this.MassiveSmallData(node);
    }

    public NetControl NetControl { get; set; }

    private async Task BenchLargeData(ClientTerminal terminal)
    {
        const int N = 4_000_000;
        var service = terminal.GetService<IBenchmarkService>();
        var data = new byte[N];

        var sw = Stopwatch.StartNew();
        var response = await service.Send(data).ResponseAsync;
        sw.Stop();

        Console.WriteLine(response);
        Console.WriteLine(sw.ElapsedMilliseconds.ToString());
    }

    private async Task PingpongSmallData(ClientTerminal terminal)
    {// 395ms (release)
        const int N = 100; // 20;
        var service = terminal.GetService<IBenchmarkService>();
        var data = new byte[100];

        var sw = Stopwatch.StartNew();
        ServiceResponse<byte[]?> response = default;
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            if (ThreadCore.Root.IsTerminated)
            {
                break;
            }

            response = await service.Pingpong(data).ResponseAsync;
            if (response.IsSuccess)
            {
                count++;
            }
        }

        sw.Stop();

        Console.WriteLine($"PingpongSmallData {count}/{N}, {sw.ElapsedMilliseconds.ToString()} ms");
        Console.WriteLine();
    }

    private async Task PingpongSmallData2(NodeAddress node)
    {// 380ms -> 340ms
        const int N = 50;
        var data = new byte[100];

        var sw = Stopwatch.StartNew();
        var count = 0;
        for (var j = 0; j < N; j++)
        {
            using (var terminal = this.NetControl.Terminal.Create(node))
            {
                var service = terminal.GetService<IBenchmarkService>();
                var response = service.Pingpong(data).ResponseAsync;
                if (response.Result.IsSuccess)
                {
                    count++;
                }
                else
                {
                    Console.WriteLine(response.Result.Result.ToString());
                }
            }
        }

        sw.Stop();

        Console.WriteLine(this.NetControl.Alternative?.MyStatus.ServerCount.ToString());
        Console.WriteLine($"PingpongSmallData2 {count}/{N}, {sw.ElapsedMilliseconds.ToString()} ms");
        Console.WriteLine();
    }

    private async Task MassiveSmallData(NodeAddress node)
    {// 1200ms (release)
        const int N = 50; //50;
        var data = new byte[100];

        ThreadPool.GetMinThreads(out var workMin, out var ioMin);
        ThreadPool.SetMinThreads(50, ioMin);

        var sw = Stopwatch.StartNew();
        var count = 0;
        Parallel.For(0, N, i =>
        {
            for (var j = 0; j < 20; j++)
            {
                using (var terminal = this.NetControl.Terminal.Create(node))
                {
                    var service = terminal.GetService<IBenchmarkService>();
                    var response = service.Pingpong(data).ResponseAsync;
                    if (response.Result.IsSuccess)
                    {
                        Interlocked.Increment(ref count);
                    }
                    else
                    {
                        Console.WriteLine(response.Result.Result.ToString());
                    }
                }
            }
        });

        sw.Stop();

        Console.WriteLine(this.NetControl.Alternative?.MyStatus.ServerCount.ToString());
        Console.WriteLine($"MassiveSmallData {count}/{N}, {sw.ElapsedMilliseconds.ToString()} ms");
        Console.WriteLine();
    }
}

public record NetbenchOptions
{
    [SimpleOption("node", description: "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
