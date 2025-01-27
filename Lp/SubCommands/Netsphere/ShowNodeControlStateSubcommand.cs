// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Stats;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("show-nodecontrol-state")]
public class ShowNodeControlStateSubcommand : ISimpleCommandAsync
{
    public ShowNodeControlStateSubcommand(ILogger<ShowNodeControlStateSubcommand> logger, IUserInterfaceService userInterfaceService, BigMachine bigMachine)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.bigMachine = bigMachine;
    }

    public async Task RunAsync(string[] args)
    {
        await this.bigMachine.NodeControlMachine.GetOrCreate().Command.ShowStatus(true);
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly BigMachine bigMachine;
}
