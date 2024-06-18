// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Stats;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("show-own-node")]
public class ShowOwnNodeSubcommand : ISimpleCommandAsync
{// Control -> context.AddSubcommand(typeof(LP.Subcommands.ShowOwnNodeSubcommand));
    public ShowOwnNodeSubcommand(ILogger<ShowOwnNodeSubcommand> logger, IUserInterfaceService userInterfaceService, NetStats netStats)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netStats = netStats;
    }

    public async Task RunAsync(string[] args)
    {
        // this.userInterfaceService.WriteLine(typeof(ShowOwnNodeSubcommand).Name);

        var node = this.netStats.GetMyNetNode();
        this.userInterfaceService.WriteLine(node.ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetStats netStats;
}
