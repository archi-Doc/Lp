// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Net;
using Arc.Unit;
using Netsphere;
using Netsphere.Packet;
using SimpleCommandLine;

namespace Sandbox;

[SimpleCommand("basic")]
public class BasicTestSubcommand : ISimpleCommandAsync<BasicTestOptions>
{
    public BasicTestSubcommand(ILogger<BasicTestSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(BasicTestOptions options, string[] args)
    {
        var nodeString = options.Node;
        if (string.IsNullOrEmpty(nodeString))
        {
            nodeString = "alternative";
        }

        if (!NetAddress.TryParse(this.logger, nodeString, out var nodeAddress))
        {
            return;
        }

        var netTerminal = this.NetControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        nodeAddress.TryCreateIPEndPoint(out var endPoint);
        endPoint = new System.Net.IPEndPoint(IPAddress.Loopback, 50000);
        var p = new PacketPing("test56789");
        var result = await packetTerminal.SendAndReceiveAsync<PacketPing, PacketPingResponse>(endPoint, p);

        using (var connection = netTerminal.TryConnect(nodeAddress))
        {
            if (connection is not null)
            {
            }
        }

        /*if (!NetAddress.TryParse(this.logger, nodeString, out var node))
        {
            return;
        }

        this.logger.TryGet()?.Log($"SendData: {node.ToString()}");
        this.logger.TryGet()?.Log($"{Stopwatch.Frequency}");

        // var nodeInformation = NodeInformation.Alternative;
        using (var terminal = this.NetControl.TerminalObsolete.TryCreate(node))
        {
            if (terminal is null)
            {
                return;
            }

            // terminal.SetMaximumResponseTime(1_000_000);
            var t = await terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(new PacketPunch());
            this.logger.TryGet()?.Log($"{t.ToString()}");
        }*/
    }

    public NetControl NetControl { get; set; }

    private ILogger<BasicTestSubcommand> logger;
}

public record BasicTestOptions
{
    [SimpleOption("node", Description = "Node address", Required = false)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
