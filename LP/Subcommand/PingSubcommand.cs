// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using LP.Net;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("ping")]
public class PingSubcommand : ISimpleCommandAsync<PingOptions>
{
    public PingSubcommand(Control control)
    {
        this.Control = control;
    }

    public async Task Run(PingOptions options, string[] args)
    {
        if (!NodeAddress.TryParse(options.Node, out var node))
        {
            Logger.Subcommand.Information($"Node parse error: {options.Node.ToString()}");
            return;
        }

        Logger.Subcommand.Information($"Ping: {node.ToString()}");

        // this.Control.Netsphere.NetStatus

        // Logger.Subcommand.Information(System.Environment.OSVersion.ToString());
    }

    public Control Control { get; set; }
}

public record PingOptions
{
    [SimpleOption("node", description: "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("count", description: "Count")]
    public int Count { get; init; } = 1;

    public override string ToString() => $"{this.Node}";
}
