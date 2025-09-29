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
    public PunchSubcommand(ILogger<PunchSubcommand> logger, NetUnit netUnit)
    {
        this.logger = logger;
        this.netUnit = netUnit;
    }

    public async Task RunAsync(PunchOptions options, string[] args)
    {
        if (!NetNode.TryParseNetNode(this.logger, options.DestinationNode, out var node))
        {
            return;
        }

        /*using (var connection = await this.netUnit.NetTerminal.Connect(node))
        {
            if (connection == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, node.ToString());
                return;
            }
        }*/

        NetAddress.TryParse(options.RelayNode, out var relayAddress);
        this.netUnit.NetTerminal.TryCreateEndpoint(ref relayAddress, EndpointResolution.PreferIpv6, out var relayEndpoint);
        if (!this.netUnit.NetStats.TryGetOwnNetNode(out var ownNode))
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.NoOwnAddress);
            return;
        }

        var ownAddress = ownNode.Address;
        if (!this.netUnit.NetTerminal.TryCreateEndpoint(ref ownAddress, EndpointResolution.PreferIpv6, out var ownEndpoint))
        {
            return;
        }

        var packetTerminal = this.netUnit.NetTerminal.PacketTerminal;
        var p = new PunchPacket(relayEndpoint, ownEndpoint);
        var result = await packetTerminal.SendAndReceive<PunchPacket, PunchPacketResponse>(node.Address, p);
        Console.WriteLine(result);
    }

    private readonly NetUnit netUnit;
    private readonly ILogger logger;
}

public record PunchOptions
{
    [SimpleOption("Destination", Description = "Destination node", Required = true)]
    public string DestinationNode { get; init; } = string.Empty;

    [SimpleOption("Relay", Description = "Relay node")]
    public string RelayNode { get; init; } = string.Empty;
}
