// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using Arc.Unit;
using LP;
using LP.NetServices;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("netbench")]
public class NetBenchSubcommand : ISimpleCommandAsync<NetBenchOptions>
{
    public NetBenchSubcommand(ILogger<NetBenchSubcommand> logger, IUserInterfaceService userInterfaceService, Control control)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.Control = control;
        this.NetControl = control.NetControl;
    }

    public async Task RunAsync(NetBenchOptions options, string[] args)
    {
        if (!NetHelper.TryParseNodeAddress(this.logger, options.Node, out var node))
        {
            return;
        }

        for (var n = 0; n < options.Count; n++)
        {
            if (this.Control.Core.IsTerminated)
            {
                break;
            }

            await this.Process(node, options);

            if (n < options.Count - 1)
            {
                this.Control.Core.Sleep(TimeSpan.FromSeconds(options.Interval), TimeSpan.FromSeconds(0.1));
            }
        }
    }

    public async Task Process(NodeAddress node, NetBenchOptions options)
    {
        this.logger.TryGet()?.Log($"NetBench: {node.ToString()}");

        var sw = Stopwatch.StartNew();
        using (var terminal = this.Control.NetControl.Terminal.Create(node))
        {
            // await this.SendLargeData(terminal);
            await this.PingpongSmallData(terminal);
        }
    }

    public Control Control { get; set; }

    public NetControl NetControl { get; set; }

    private async Task SendLargeData(ClientTerminal terminal)
    {
        const int N = 4_000_000;
        var service = terminal.GetService<IBenchmarkService>();
        var data = new byte[N];

        var sw = Stopwatch.StartNew();
        var response = await service.Send(data).ResponseAsync;
        sw.Stop();

        this.userInterfaceService.WriteLine(response.ToString());
        this.userInterfaceService.WriteLine(sw.ElapsedMilliseconds.ToString());
    }

    private async Task PingpongSmallData(ClientTerminal terminal)
    {
        const int N = 20;
        var service = terminal.GetService<IBenchmarkService>();
        var data = new byte[100];

        var sw = Stopwatch.StartNew();
        ServiceResponse<byte[]?> response = default;
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            response = await service.Pingpong(data).ResponseAsync;
            if (response.IsSuccess)
            {
                count++;
            }
        }

        sw.Stop();

        this.userInterfaceService.WriteLine($"PingpongSmallData {count}/{N}, {sw.ElapsedMilliseconds.ToString()} ms");
        this.userInterfaceService.WriteLine();
    }

    private async Task MassiveSmallData(NodeAddress node)
    {
        const int N = 50;
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
                        this.userInterfaceService.WriteLine(response.Result.Result.ToString());
                    }
                }
            }
        });

        sw.Stop();

        this.userInterfaceService.WriteLine(this.NetControl.Alternative?.MyStatus.ServerCount.ToString());
        this.userInterfaceService.WriteLine($"MassiveSmallData {count}/{N}, {sw.ElapsedMilliseconds.ToString()} ms");
        this.userInterfaceService.WriteLine();
    }

    private ILogger<NetBenchSubcommand> logger;
    private IUserInterfaceService userInterfaceService;
}

public record NetBenchOptions
{
    [SimpleOption("node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("count", Description = "Count")]
    public int Count { get; init; } = 1;

    [SimpleOption("interval", Description = "Interval (seconds)")]
    public int Interval { get; init; } = 2;

    public override string ToString() => $"{this.Node}";
}
