// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

[SimpleCommand("show-merger-key")]
public class ShowMergerKeySubcommand : ISimpleCommandAsync
{
    public ShowMergerKeySubcommand(ILogger<ShowMergerKeySubcommand> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(string[] args)
    {
        if (await this.nestedcommand.RobustConnection.Get(this.logger) is not { } connection)
        {
            return;
        }

        var service = connection.GetService<LpDogmaNetService>();
        var r = await service.GetMergerKey();

        this.logger.TryGet()?.Log($"{r.ToString()}");
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
}
