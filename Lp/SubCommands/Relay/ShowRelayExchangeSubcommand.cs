// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.Relay;

[SimpleCommand("show-relay-exchange", Description = "Display relay exchange.")]
public class ShowRelayExchangeSubcommand : ISimpleCommand
{
    public ShowRelayExchangeSubcommand(ILogger<ShowRelayExchangeSubcommand> logger, IUserInterfaceService userInterfaceService, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netTerminal = netTerminal;
    }

    public async Task Execute(string[] args, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Show relay exchange");

        this.userInterfaceService.WriteLine(this.netTerminal.RelayAgent.ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetTerminal netTerminal;
}
