// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Packet;
using Netsphere.Relay;
using SimpleCommandLine;

namespace Netsphere.Version;

[SimpleCommand("get")]
internal class GetCommand : ISimpleCommandAsync<GetOptions>
{
    public GetCommand(ILogger<GetCommand> logger, ProgramUnit.Unit unit)
    {
        this.logger = logger;
        this.unit = unit;
    }

    public async Task RunAsync(GetOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"Netsphere.Version get: {options.ToString()}");

        if (!NetNode.TryParseNetNode(this.logger, options.Node, out var node))
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Cannot parse node: {options.Node}");
        }

        var netOptions = new NetOptions() with
        {
        };

        await this.unit.Run(netOptions, false);

        var p = new PingPacket("test56789");
        var netTerminal = this.unit.Context.ServiceProvider.GetRequiredService<NetTerminal>();
        var result = await netTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(node.Address, p);

        await this.unit.Terminate();
    }

    private readonly ILogger logger;
    private readonly ProgramUnit.Unit unit;
}
