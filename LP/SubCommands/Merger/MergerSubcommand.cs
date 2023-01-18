// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using Arc.Unit;
using LP;
using LP.NetServices;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("merger")]
public class MergerSubcommand : ISimpleCommandAsync<MergerSubcommandOptions>
{
    public MergerSubcommand(ILogger<MergerSubcommand> logger, IUserInterfaceService userInterfaceService, MergerNestedcommand nestedcommand)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(MergerSubcommandOptions options, string[] args)
    {
        if (!NetHelper.TryParseNodeInformation(this.logger, options.Node, out var node))
        {
            return;
        }

        this.nestedcommand.Node = node;
        this.userInterfaceService.WriteLine(node.ToString());
        await this.nestedcommand.MainAsync();
    }

    private ILogger<MergerSubcommand> logger;
    private IUserInterfaceService userInterfaceService;
    private MergerNestedcommand nestedcommand;
}

public record MergerSubcommandOptions
{
    [SimpleOption("node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
