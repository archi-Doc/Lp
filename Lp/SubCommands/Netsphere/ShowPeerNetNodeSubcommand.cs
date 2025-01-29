// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Stats;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("show-peer-netnode")]
public class ShowPeerNetNodeSubcommand : ISimpleCommandAsync
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.ShowOwnNodeSubcommand));
    public ShowPeerNetNodeSubcommand(ILogger<ShowPeerNetNodeSubcommand> logger, IUserInterfaceService userInterfaceService, NetStats netStats)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netStats = netStats;
    }

    public async Task RunAsync(string[] args)
    {
        var node = this.netStats.PeerNetNode;
        this.userInterfaceService.WriteLine($"Peer Netnode: {node?.ToString()}");
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetStats netStats;
}
