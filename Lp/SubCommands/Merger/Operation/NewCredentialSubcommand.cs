// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerOperation;

[SimpleCommand("new-credential")]
public class NewCredentialSubcommand : ISimpleCommandAsync<MergerSubcommandOptions>
{
    public NewCredentialSubcommand(ILogger<MergerClientSubcommand> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task RunAsync(MergerSubcommandOptions options, string[] args)
    {
        if (!NetNode.TryParseNetNode(this.logger, options.Node, out var node))
        {
            return;
        }

        this.userInterfaceService.WriteLine(node.ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
}

public record MergerOperationOptions
{
    [SimpleOption("Node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;
}
