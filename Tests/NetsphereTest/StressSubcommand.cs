// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.IO;
using Arc.Unit;
using LP.NetServices;
using SimpleCommandLine;

namespace NetsphereTest;

[SimpleCommand("stress")]
public class StressSubcommand : ISimpleCommandAsync<StressOptions>
{
    public StressSubcommand(ILogger<StressSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(StressOptions options, string[] args)
    {
        NodeAddress? node = NodeAddress.Alternative;
        if (!string.IsNullOrEmpty(options.Node))
        {
            if (!NetHelper.TryParseNodeAddress(this.logger, options.Node, out node))
            {
                return;
            }
        }

        this.logger.TryGet()?.Log($"Stress: {node.ToString()}, Total/Concurrent: {options.Total}/{options.Concurrent}");

        await this.Stress1(node, options);
    }

    public NetControl NetControl { get; set; }

    private async Task Stress1(NodeAddress node, StressOptions options)
    {
        var data = new byte[100];
        int successCount = 0;
        int failureCount = 0;
        long totalLatency = 0;

        // ThreadPool.GetMinThreads(out var workMin, out var ioMin);
        // ThreadPool.SetMinThreads(Concurrent, ioMin);

        var sw = Stopwatch.StartNew();
        Parallel.For(0, options.Concurrent, i =>
        {
            for (var j = 0; j < (options.Total / options.Concurrent); j++)
            {
                var sw2 = new Stopwatch();
                using (var terminal = this.NetControl.Terminal.Create(node))
                {
                    var service = terminal.GetService<IBenchmarkService>();
                    sw2.Restart();
                    var response = service.Pingpong(data).ResponseAsync;

                    if (response.Result.IsSuccess)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref failureCount);
                    }

                    sw2.Stop();
                    Interlocked.Add(ref totalLatency, sw2.ElapsedMilliseconds);
                }
            }
        });

        sw.Stop();

        var record = new IBenchmarkService.ReportRecord()
        {
            SuccessCount = successCount,
            FailureCount = failureCount,
            Concurrent = options.Concurrent,
            ElapsedMilliseconds = sw.ElapsedMilliseconds,
            CountPerSecond = (int)((successCount + failureCount) * 1000 / sw.ElapsedMilliseconds),
            AverageLatency = (int)(totalLatency / (successCount + failureCount)),
        };

        using (var terminal = this.NetControl.Terminal.Create(node))
        {
            var service = terminal.GetService<IBenchmarkService>();
            await service.Report(record);
        }
    }

    private ILogger logger;
}

public record StressOptions
{
    [SimpleOption("node", Description = "Node address")]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("total", Description = "")]
    public int Total { get; init; } = 1_000;

    [SimpleOption("concurrent", Description = "")]
    public int Concurrent { get; init; } = 25;

    public override string ToString() => $"{this.Node}";
}
