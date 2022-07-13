// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("punch")]
public class PunchSubcommand : ISimpleCommandAsync<PunchOptions>
{
    public PunchSubcommand(Control control)
    {
        this.Control = control;
    }

    public async Task RunAsync(PunchOptions options, string[] args)
    {
        if (!SubcommandService.TryParseNodeAddress(options.Node, out var node))
        {
            return;
        }

        NodeAddress? nextNode = null;
        if (!string.IsNullOrEmpty(options.NextNode))
        {
            SubcommandService.TryParseNodeAddress(options.NextNode, out nextNode);
        }

        for (var n = 0; n < options.Count; n++)
        {
            if (this.Control.Core.IsTerminated)
            {
                break;
            }

            await this.Punch(node, nextNode, options);

            if (n < options.Count - 1)
            {
                this.Control.Core.Sleep(TimeSpan.FromSeconds(options.Interval), TimeSpan.FromSeconds(0.1));
            }
        }
    }

    public async Task Punch(NodeAddress node, NodeAddress? nextNode, PunchOptions options)
    {
        Logger.Default.Information($"Punch: {node.ToString()}");

        var sw = Stopwatch.StartNew();
        using (var terminal = this.Control.NetControl.Terminal.Create(node))
        {
            var p = new PacketPunch(nextNode?.CreateEndpoint());

            sw.Restart();
            var result = await terminal.SendPacketAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            sw.Stop();
            if (result.Value != null)
            {
                Logger.Default.Information($"Received: {result.ToString()} - {sw.ElapsedMilliseconds} ms");
            }
            else
            {
                Logger.Default.Error($"{result}");
            }
        }
    }

    public Control Control { get; set; }
}

public record PunchOptions
{
    [SimpleOption("node", description: "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("next", description: "Next node address")]
    public string NextNode { get; init; } = string.Empty;

    [SimpleOption("count", description: "Count")]
    public int Count { get; init; } = 1;

    [SimpleOption("interval", description: "Interval (seconds)")]
    public int Interval { get; init; } = 2;

    public override string ToString() => $"{this.Node}";
}
