﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("get")]
public class NodeSubcommandGet : ISimpleCommandAsync<NodeSubcommandGetOptions>
{
    public NodeSubcommandGet(ILogger<NodeSubcommandGet> logger, Control control)
    {
        this.Control = control;
        this.logger = logger;
    }

    public async Task RunAsync(NodeSubcommandGetOptions options, string[] args)
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
                this.logger.TryGet()?.Log($"{n.ToString()}");
            }
        }
    }

    public Control Control { get; set; }

    private ILogger logger;
}

public record NodeSubcommandGetOptions
{
    [SimpleOption("node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;
}