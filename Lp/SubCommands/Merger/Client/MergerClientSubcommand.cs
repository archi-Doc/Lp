// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("merger-client")]
public class MergerClientSubcommand : ISimpleCommandAsync<MergerSubcommandOptions>
{
    public MergerClientSubcommand(ILogger<MergerClientSubcommand> logger, IUserInterfaceService userInterfaceService, MergerClientNestedCommand nestedcommand)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(MergerSubcommandOptions options, string[] args)
    {
        if (!NetNode.TryParseNetNode(this.logger, options.Node, out var node))
        {
            return;
        }

        this.nestedcommand.Node = node;
        this.userInterfaceService.WriteLine(node.ToString());
        await this.nestedcommand.MainAsync();
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly MergerClientNestedCommand nestedcommand;
}

public record MergerSubcommandOptions
{
    [SimpleOption("Node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
