// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Relay;
using SimpleCommandLine;

namespace Lp.Subcommands.Relay;

[SimpleCommand("show-relay-information", Description = "Display relay information")]
public class ShowRelayInformationSubcommand : ISimpleCommandAsync
{
    public ShowRelayInformationSubcommand(ILogger<ShowRelayInformationSubcommand> logger, IUserInterfaceService userInterfaceService, NetTerminal netTerminal, IRelayControl relayControl)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netTerminal = netTerminal;
        this.relayControl = relayControl;
    }

    public async Task RunAsync(string[] args)
    {// this.logger.TryGet()?.Log
        this.userInterfaceService.WriteLine("Relay information");
        this.userInterfaceService.WriteLine($"Relay control: {this.relayControl.GetType().Name}");

        // Relay circuit
        this.userInterfaceService.WriteLine($"Relay circuit (Client): ");
        this.userInterfaceService.WriteLine(this.netTerminal.IncomingCircuit.toString());

        // Relay exchanges
        this.userInterfaceService.WriteLine($"Relay exchanges (Server): ");
        this.userInterfaceService.WriteLine(this.netTerminal.RelayAgent.ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetTerminal netTerminal;
    private readonly IRelayControl relayControl;
}
