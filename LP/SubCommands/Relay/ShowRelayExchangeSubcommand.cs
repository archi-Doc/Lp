// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands.Relay;

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

        // var st = $"Number of relay exchange: {this.netTerminal.RelayAgent.NumberOfExchanges}";
        // this.userInterfaceService.WriteLine(st);
        this.netTerminal.RelayAgent.Show();
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetTerminal netTerminal;
}
