// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

[SimpleCommand("new-credential")]
public class NewCredentialSubcommand : ISimpleCommandAsync<CommandOptions>
{
    public NewCredentialSubcommand(ILogger<MergerClient.Command> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task RunAsync(CommandOptions options, string[] args)
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
