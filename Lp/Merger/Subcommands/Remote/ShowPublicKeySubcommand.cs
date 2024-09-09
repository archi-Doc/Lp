// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

[SimpleCommand("show-publickey")]
public class ShowPublicKeySubcommand : ISimpleCommandAsync
{
    public ShowPublicKeySubcommand(ILogger<ShowPublicKeySubcommand> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(string[] args)
    {
        if (await this.nestedcommand.RobustConnection.GetConnection(this.logger) is not { } connection)
        {
            return;
        }

        var service = connection.GetService<IMergerRemote>();
        var r = await service.GetPublicKey();

        this.logger.TryGet()?.Log($"{r.ToString()}");
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
}
