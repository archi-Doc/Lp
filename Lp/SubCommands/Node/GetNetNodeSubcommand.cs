// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("get-net-node")]
public class GetNetNodeSubcommand : ISimpleCommandAsync<GetNetNodeOptions>
{
    public GetNetNodeSubcommand(ILogger<GetNetNodeSubcommand> logger, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.netTerminal = netTerminal;
    }

    public async Task RunAsync(GetNetNodeOptions options, string[] args)
    {
        if (!NetAddress.TryParse(this.logger, options.Address, out var address))
        {
            return;
        }

        var node = await this.netTerminal.UnsafeGetNetNode(address);
        if (node is not null)
        {
            this.logger.TryGet()?.Log($"{node.ToString()}");
        }
    }

    private readonly ILogger logger;
    private readonly NetTerminal netTerminal;
}

public record GetNetNodeOptions
{
    [SimpleOption("address", Description = "Node address", Required = true)]
    public string Address { get; init; } = string.Empty;
}
