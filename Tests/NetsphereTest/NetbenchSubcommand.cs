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
            

            await this.BenchLargeData(terminal);
        }
    }

    public NetControl NetControl { get; set; }

    private async Task BenchLargeData(ClientTerminal terminal)
    {
        var service = terminal.GetService<IBenchmarkService>();
        var data = new byte[4_000_000];

        var sw = Stopwatch.StartNew();
        var response = await service.Send(data).ResponseAsync;
        sw.Stop();

        Console.WriteLine(response);
        Console.WriteLine(sw.ElapsedMilliseconds.ToString());
    }
}

public record NetbenchOptions
{
    [SimpleOption("node", description: "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
