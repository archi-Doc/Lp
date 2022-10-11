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
    public PunchSubcommand(ILogger<PunchSubcommand> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public async Task RunAsync(PunchOptions options, string[] args)
    {
        if (!SubcommandService.TryParseNodeAddress(this.logger, options.Node, out var node))
        {
            return;
        }

        NodeAddress? nextNode = null;
        if (!string.IsNullOrEmpty(options.NextNode))
        {
            SubcommandService.TryParseNodeAddress(this.logger, options.NextNode, out nextNode);
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
        this.logger.TryGet()?.Log($"Punch: {node.ToString()}");

        var sw = Stopwatch.StartNew();
        using (var terminal = this.Control.NetControl.Terminal.Create(node))
        {
            var p = new PacketPunch(nextNode?.CreateEndpoint());

            sw.Restart();
            var result = await terminal.SendPacketAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            sw.Stop();
            if (result.Value != null)
            {
                this.logger.TryGet()?.Log($"Received: {result.ToString()} - {sw.ElapsedMilliseconds} ms");
            }
            else
            {
                this.logger.TryGet(LogLevel.Error)?.Log($"{result}");
            }
        }
    }

    public Control Control { get; set; }

    private ILogger<PunchSubcommand> logger;
}

public record PunchOptions
{
    [SimpleOption("node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("next", Description = "Next node address")]
    public string NextNode { get; init; } = string.Empty;

    [SimpleOption("count", Description = "Count")]
    public int Count { get; init; } = 1;

    [SimpleOption("interval", Description = "Interval (seconds)")]
    public int Interval { get; init; } = 2;

    public override string ToString() => $"{this.Node}";
}
