// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.Relay;

[SimpleCommand("show-relay-exchange", Description = "Display relay exchange.")]
public class ShowRelayExchangeSubcommand : ISimpleCommandAsync
{
    public ShowRelayExchangeSubcommand(ILogger<ShowRelayExchangeSubcommand> logger, IUserInterfaceService userInterfaceService, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netTerminal = netTerminal;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log("Show relay exchange");

        this.userInterfaceService.WriteLine(this.netTerminal.RelayAgent.ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetTerminal netTerminal;
}
