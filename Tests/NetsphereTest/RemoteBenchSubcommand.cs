// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
        if (!NetNode.TryParseNetNode(this.logger, options.Node, out var node))
        {
            return;
        }

        using (var terminal = this.netControl.TerminalObsolete.TryCreate(node))
        {
            if (terminal is null)
            {
                return;
            }

            var service = terminal.GetService<IBenchmarkService>();
            if (await service.Register() == NetResult.Success)
            {
                Console.WriteLine($"Register: Success");

            }
            else
            {
                Console.WriteLine($"Register: Failure");
                return;
            }
        }

        while (true)
        {
            Console.WriteLine($"Waiting...");
            if (await this.remoteBenchBroker.Wait() == false)
            {
                Console.WriteLine($"Exit");
                break;
            }

            Console.WriteLine($"Benchmark {node.ToString()}, Total/Concurrent: {this.remoteBenchBroker.Total}/{this.remoteBenchBroker.Concurrent}");
            await this.remoteBenchBroker.Process(netControl.TerminalObsolete, node);
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
