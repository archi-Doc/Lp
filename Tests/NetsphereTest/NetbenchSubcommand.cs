// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Unit;
using LP.NetServices;
using SimpleCommandLine;

namespace NetsphereTest;

[SimpleCommand("bench")]
public class NetbenchSubcommand : ISimpleCommandAsync<NetbenchOptions>
{
    public NetbenchSubcommand(ILogger<NetbenchSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(NetbenchOptions options, string[] args)
    {
        NetNode? node = NetNode.Alternative;
        if (!string.IsNullOrEmpty(options.Node))
        {
            if (!NetNode.TryParseNetNode(this.logger, options.Node, out node))
            {
                return;
            }
        }

        this.logger.TryGet()?.Log($"Netbench: {node.ToString()}");

        // var nodeInformation = NodeInformation.Alternative;
        using (var terminal = this.NetControl.TerminalObsolete.TryCreate(node))
        {
            /*var p = new PacketPunch(null);
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
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms");*/

            // await this.BenchLargeData(terminal); // 1060 ms
            // await this.PingpongSmallData(terminal); // 350 ms

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

        await this.PingpongSmallData2(node);
        // await this.MassiveSmallData(node); // 1000 ms
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
    {
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

    private async Task PingpongSmallData2(NetNode node)
    {// 380ms -> 340ms
        const int N = 50;
        var data = new byte[100];

        var sw = Stopwatch.StartNew();
        var count = 0;
        for (var j = 0; j < N; j++)
        {
            using (var terminal = this.NetControl.TerminalObsolete.TryCreate(node))
            {
                if (terminal is null)
                {
                    return;
                }
                var service = terminal.GetService<IBenchmarkService>();
                var response = await service.Pingpong(data).ResponseAsync;
                if (response.IsSuccess)
                {
                    count++;
                }
                else
                {
                    Console.WriteLine(response.Result.ToString());
                }

                /*var response = service.Pingpong(data).ResponseAsync;
                if (response.Result.IsSuccess)
                {
                    count++;
                }
                else
                {
                    Console.WriteLine(response.Result.Result.ToString());
                }*/
            }
        }

        sw.Stop();

       // Console.WriteLine(this.NetControl.Alternative?.MyStatus.ServerCount.ToString());
        Console.WriteLine($"PingpongSmallData2 {count}/{N}, {sw.ElapsedMilliseconds.ToString()} ms");
        Console.WriteLine();
    }

    private async Task MassiveSmallData(NetNode node)
    {// 1200ms (release)
        const int Total = 1000;
        const int Concurrent = 50; //50;
        var data = new byte[100];

        // ThreadPool.GetMinThreads(out var workMin, out var ioMin);
        // ThreadPool.SetMinThreads(Concurrent, ioMin);

        var sw = Stopwatch.StartNew();
        var count = 0;
        Parallel.For(0, Concurrent, i =>
        {
            for (var j = 0; j < (Total / Concurrent); j++)
            {
                using (var terminal = this.NetControl.TerminalObsolete.TryCreate(node))
                {
                    if (terminal is null)
                    {
                        return;
                    }

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

        // ThreadPool.SetMinThreads(workMin, ioMin);

        sw.Stop();

        // Console.WriteLine(this.NetControl.Alternative?.MyStatus.ServerCount.ToString());
        Console.WriteLine($"MassiveSmallData {count}/{Concurrent}, {sw.ElapsedMilliseconds.ToString()} ms");
        Console.WriteLine();
    }

    private ILogger<NetbenchSubcommand> logger;
}

public record NetbenchOptions
{
    [SimpleOption("node", Description = "Node address")]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
