// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Unit;
using LP.NetServices;
using SimpleCommandLine;

namespace NetsphereTest;

[SimpleCommand("remotebench")]
public class RemoteBenchSubcommand : ISimpleCommandAsync<RemoteBenchOptions>
{
    public RemoteBenchSubcommand(ILogger<RemoteBenchSubcommand> logger, NetControl netControl, RemoteBenchBroker remoteBenchBroker)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.remoteBenchBroker = remoteBenchBroker;
    }

    public async Task RunAsync(RemoteBenchOptions options, string[] args)
    {
        if (!NetNode.TryParse(options.Node, out var node))
        {// NetNode.TryParseNetNode(this.logger, options.Node, out var node)
            return;
        }

        await Console.Out.WriteLineAsync("Wait about 3 seconds for the execution environment to stabilize.");
        try
        {
            await Task.Delay(3_000, ThreadCore.Root.CancellationToken);
        }
        catch
        {
            return;
        }

        // await this.TestPingpong(node);

        using (var connection = await this.netControl.NetTerminal.TryConnect(node))
        {
            if (connection is null)
            {
                return;
            }

            var service = connection.GetService<IBenchmarkService>();
            if (await service.Register() == NetResult.Success)
            {
                this.logger.TryGet()?.Log($"Register: Success");

            }
            else
            {
                this.logger.TryGet()?.Log($"Register: Failure");
                return;
            }
        }

        while (true)
        {
            this.logger.TryGet()?.Log($"Waiting...");
            if (await this.remoteBenchBroker.Wait() == false)
            {
                Console.WriteLine($"Exit");
                break;
            }

            this.logger.TryGet()?.Log($"Benchmark {node.ToString()}, Total/Concurrent: {this.remoteBenchBroker.Total}/{this.remoteBenchBroker.Concurrent}");
            await this.remoteBenchBroker.Process(netControl.NetTerminal, node);
        }
    }

    private async Task TestPingpong(NetNode node)
    {
        const int N = 100;

        using (var connection = await this.netControl.NetTerminal.TryConnect(node))
        {
            if (connection is null)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            var service = connection.GetService<IBenchmarkService>();
            for (var i = 0; i < N; i++)
            {
                await service.Pingpong([0, 1, 2,]);
            }

            sw.Stop();

            this.logger.TryGet()?.Log($"Pingpong x {N} {sw.ElapsedMilliseconds} ms");
        }
    }

    private NetControl netControl { get; set; }
    private ILogger logger;
    private RemoteBenchBroker remoteBenchBroker;
}

public record RemoteBenchOptions
{
    [SimpleOption("node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
