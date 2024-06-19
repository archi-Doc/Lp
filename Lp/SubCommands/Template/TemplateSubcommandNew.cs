// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("template-subcommand")]
public class TemplateSubcommand : ISimpleCommandAsync
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.ShowOwnNodeSubcommand));
    public TemplateSubcommand(ILogger<TemplateSubcommand> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task RunAsync(string[] args)
    {
        // this.userInterfaceService.WriteLine(typeof(TemplateSubcommand).Name);

        // var node = this.netStats.GetMyNetNode();
        // this.userInterfaceService.WriteLine(node.ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
}
