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
        NodeAddress? node;

        if (string.Compare(options.Node, "alternative", true) == 0)
        {
            node = NodeAddress.Alternative;
        }
        else
        {
            if (!NodeAddress.TryParse(options.Node, out node))
            {
                Logger.Subcommand.Error($"Could not parse: {options.Node.ToString()}");
                return;
            }

            if (!node.IsValid())
            {
                Logger.Subcommand.Error($"Invalid node address: {options.Node.ToString()}");
                return;
            }
        }

        for (var n = 0; n < options.Count; n++)
        {
            if (this.Control.Core.IsTerminated)
            {
                break;
            }

            await this.Ping(node, options);

            if (n < options.Count - 1)
            {
                this.Control.Core.Sleep(TimeSpan.FromSeconds(options.Interval), TimeSpan.FromSeconds(0.1));
            }
        }
    }

    public async Task Ping(NodeAddress node, PingOptions options)
    {
        Logger.Subcommand.Information($"Ping: {node.ToString()}");

        using (var terminal = this.Control.Netsphere.Terminal.Create(node))
        {
            var p = new PacketPing(this.Control.Netsphere.NetStatus.GetMyNodeInformation(), "test");
            terminal.SendUnmanaged(p, PacketId.Ping);

            p = terminal.Receive<PacketPing>();
            if (p == null)
            {
                Logger.Subcommand.Information($"Timeout.");
            }
            else
            {
                Logger.Subcommand.Information($"{p.ToString()}");
            }
        }

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

    [SimpleOption("interval", description: "Interval (seconds)")]
    public int Interval { get; init; } = 2;

    public override string ToString() => $"{this.Node}";
}
