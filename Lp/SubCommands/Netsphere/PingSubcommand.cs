// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere.Packet;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("ping")]
public class PingSubcommand : ISimpleCommandAsync<PingOptions>
{
    public PingSubcommand(ILogger<PingSubcommand> logger, Control control)
    {
        this.Control = control;
        this.logger = logger;
    }

    public async Task RunAsync(PingOptions options, string[] args)
    {
        if (!NetAddress.TryParse(this.logger, options.Node, out var address))
        {
            return;
        }

        for (var n = 0; n < options.Count; n++)
        {
            if (this.Control.Core.IsTerminated)
            {
                break;
            }

            await this.Ping(address, options);

            if (n < options.Count - 1)
            {
                this.Control.Core.Sleep(TimeSpan.FromSeconds(options.Interval), TimeSpan.FromSeconds(0.1));
            }
        }
    }

    public async Task Ping(NetAddress address, PingOptions options)
    {
        this.logger.TryGet()?.Log($"Ping: {address.ToString()}");

        var packetTerminal = this.Control.NetControl.NetTerminal.PacketTerminal;

        var sw = Stopwatch.StartNew();
        var p = new PingPacket("test56789");
        var result = await packetTerminal.SendAndReceive<PingPacket, PingPacketResponse>(address, p, 0, default, EndpointResolution.NetAddress);

        sw.Stop();
        if (result.Value is not null)
        {
            this.logger.TryGet()?.Log($"Received: {result.ToString()} - {sw.ElapsedMilliseconds} ms");
            this.logger.TryGet()?.Log(result.Value.ToString());

            if (result.Value.SourceEndpoint.EndPoint is { } endpoint)
            {
                this.Control.NetControl.NetStats.ReportEndpoint(endpoint);
            }
        }
        else
        {
            this.logger.TryGet(LogLevel.Error)?.Log($"{result}");
        }
    }

    public Control Control { get; set; }

    private ILogger logger;
}

public record PingOptions
{
    [SimpleOption("Node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("Count", Description = "Count")]
    public int Count { get; init; } = 1;

    [SimpleOption("Interval", Description = "Interval (seconds)")]
    public int Interval { get; init; } = 2;

    public override string ToString() => $"{this.Node}";
}
