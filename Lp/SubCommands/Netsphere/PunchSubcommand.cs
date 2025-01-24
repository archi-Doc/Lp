// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Net;
using Netsphere;
using Netsphere.Packet;
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
        if (!NetNode.TryParseNetNode(this.logger, options.DestinationNode, out var node))
        {
            return;
        }

        /*using (var connection = await this.netControl.NetTerminal.Connect(node))
        {
            if (connection == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, node.ToString());
                return;
            }
        }*/

        NetAddress.TryParse(options.RelayNode, out var relayAddress);
        this.netControl.NetTerminal.TryCreateEndpoint(ref relayAddress, EndpointResolution.PreferIpv6, out var relayEndpoint);
        if (!this.netControl.NetStats.TryGetOwnNetNode(out var ownNode))
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.NoOwnAddress);
            return;
        }

        var ownAddress = ownNode.Address;
        if (!this.netControl.NetTerminal.TryCreateEndpoint(ref ownAddress, EndpointResolution.PreferIpv6, out var ownEndpoint))
        {
            return;
        }

        var packetTerminal = this.netControl.NetTerminal.PacketTerminal;
        var p = new PunchPacket(relayEndpoint, ownEndpoint);
        var result = await packetTerminal.SendAndReceive<PunchPacket, PunchPacketResponse>(node.Address, p);
        Console.WriteLine(result);
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
    [SimpleOption("Destination", Description = "Destination node", Required = true)]
    public string DestinationNode { get; init; } = string.Empty;

    [SimpleOption("Relay", Description = "Relay node")]
    public string RelayNode { get; init; } = string.Empty;
}
