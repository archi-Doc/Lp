// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("getnode")]
public class GetNodeInformationSubcommand : ISimpleCommandAsync<PingOptions>
{
    public GetNodeInformationSubcommand(ILogger<GetNodeInformationSubcommand> logger, Control control)
    {
        this.Control = control;
        this.logger = logger;
    }

    public async Task RunAsync(PingOptions options, string[] args)
    {
        if (!NetHelper.TryParseNodeAddress(this.logger, options.Node, out var node))
        {
            return;
        }

        using (var terminal = this.Control.NetControl.Terminal.Create(node))
        {
            var p = new PacketGetNodeInformation();
            var result = await terminal.SendPacketAndReceiveAsync<PacketGetNodeInformation, PacketGetNodeInformationResponse>(p);
            if (result.Value != null)
            {
                var n = NodeInformation.Merge(node, result.Value.Node);
                this.logger.TryGet()?.Log($"NodeInformation: {n.ToString()}");
            }
        }
    }

    public Control Control { get; set; }

    private ILogger logger;
}

public record GetNodeInformationSubcommandOptions
{
    [SimpleOption("node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;
}
