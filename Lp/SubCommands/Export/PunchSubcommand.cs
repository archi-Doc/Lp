// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("punch")]
public class PunchSubcommand : ISimpleCommandAsync<PunchOptions>
{
    public PunchSubcommand(ILogger<PunchSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.netControl = netControl;
    }

    public async Task RunAsync(PunchOptions options, string[] args)
    {
        if (!NetNode.TryParseNetNode(this.logger, options.Node, out var node))
        {
            return;
        }

        using (var connection = await this.netControl.NetTerminal.Connect(node))
        {
            if (connection == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, node.ToString());
                return;
            }

            // connection.SendAndReceive
        }
    }

    public async Task Punch(NetAddress node, NetAddress nextNode, PunchOptions options)
    {
        this.logger.TryGet()?.Log($"Punch: {node.ToString()}");

        /*var sw = Stopwatch.StartNew();
        using (var terminal = this.Control.NetControl.TerminalObsolete.TryCreate(node))
        {
            NetEndPoint endPoint;
            if (terminal is null)
            {
                return;
            }
            else if (this.Control.NetControl.TerminalObsolete.TryCreateEndPoint(nextNode, out endPoint))
            {
                return;
            }

            var p = new PacketPunchObsolete(endPoint.EndPoint);

            sw.Restart();
            var result = await terminal.SendPacketAndReceiveAsync<PacketPunchObsolete, PacketPunchResponseObsolete>(p);
            sw.Stop();
            if (result.Value != null)
            {
                this.logger.TryGet()?.Log($"Received: {result.ToString()} - {sw.ElapsedMilliseconds} ms");
            }
            else
            {
                this.logger.TryGet(LogLevel.Error)?.Log($"{result}");
            }
        }*/
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
}

public record PunchOptions
{
    [SimpleOption("Node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("Next", Description = "Next node address")]
    public string NextNode { get; init; } = string.Empty;

    [SimpleOption("Count", Description = "Count")]
    public int Count { get; init; } = 1;

    [SimpleOption("Interval", Description = "Interval (seconds)")]
    public int Interval { get; init; } = 2;

    public override string ToString() => $"{this.Node}";
}
