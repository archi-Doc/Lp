// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Stats;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("show-own-netnode")]
public class ShowOwnNetNodeSubcommand : ISimpleCommandAsync
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.ShowOwnNodeSubcommand));
    public ShowOwnNetNodeSubcommand(ILogger<ShowOwnNetNodeSubcommand> logger, IUserInterfaceService userInterfaceService, NetStats netStats)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netStats = netStats;
    }

    public async Task RunAsync(string[] args)
    {
        var node = this.netStats.GetOwnNetNode();
        this.userInterfaceService.WriteLine($"{this.netStats.GetOwnNodeType().ToString()}: {node.ToString()}");
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetStats netStats;
}
