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
    public MergerSubcommand(ILogger<MergerSubcommand> logger, MergerNestedcommand nestedcommand)
    {
        this.logger = logger;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(MergerSubcommandOptions options, string[] args)
    {
        if (!NetHelper.TryParseNodeAddress(this.logger, options.Node, out var node))
        {
            return;
        }

        await this.nestedcommand.MainAsync();
    }

    private ILogger<MergerSubcommand> logger;
    private MergerNestedcommand nestedcommand;
}

public record MergerSubcommandOptions
{
    [SimpleOption("node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
