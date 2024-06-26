﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.Relay;

[SimpleCommand("show-incoming-relay", Description = "Check the incoming relay circuit and display detailed status.")]
public class ShowIncomingRelaySubcommand : ISimpleCommandAsync
{
    public ShowIncomingRelaySubcommand(ILogger<ShowIncomingRelaySubcommand> logger, IUserInterfaceService userInterfaceService, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netTerminal = netTerminal;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log("Show incoming relay circuit");

        var st = await this.netTerminal.IncomingCircuit.UnsafeDetailedToString();
        this.userInterfaceService.WriteLine(st);
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetTerminal netTerminal;
}
