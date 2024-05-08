// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands.Relay;

[SimpleCommand("show-relay", Description = "Check the relay circuit and display detailed status.")]
public class ShowRelaySubcommand : ISimpleCommandAsync
{
    public ShowRelaySubcommand(ILogger<ShowRelaySubcommand> logger, IUserInterfaceService userInterfaceService, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netTerminal = netTerminal;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log("Show relay circuit");

        var st = await this.netTerminal.RelayCircuit.UnsafeDetailedToString();
        this.userInterfaceService.WriteLine(st);
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetTerminal netTerminal;
}
