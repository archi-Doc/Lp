// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Relay;
using SimpleCommandLine;

namespace Lp.Subcommands.Relay;

[SimpleCommand("show-relay-circuit", Description = "Display relay information")]
public class ShowRelayCircuitSubcommand : ISimpleCommandAsync
{
    public ShowRelayCircuitSubcommand(ILogger<ShowRelayCircuitSubcommand> logger, IUserInterfaceService userInterfaceService, NetTerminal netTerminal, IRelayControl relayControl)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netTerminal = netTerminal;
        this.relayControl = relayControl;
    }

    public async Task RunAsync(string[] args)
    {// this.logger.TryGet()?.Log
        string st;
        this.userInterfaceService.WriteLine("Relay information");
        this.userInterfaceService.WriteLine($"Relay control: {this.relayControl.GetType().Name}");

        // Relay circuit
        st = this.netTerminal.IncomingCircuit.UnsafeToString();
        if (!string.IsNullOrEmpty(st))
        {
            this.userInterfaceService.WriteLine($"Incoming relay circuit (Client): ");
            this.userInterfaceService.WriteLine(st);
        }

        st = this.netTerminal.OutgoingCircuit.UnsafeToString();
        if (!string.IsNullOrEmpty(st))
        {
            this.userInterfaceService.WriteLine($"Outgoing relay circuit (Client): ");
            this.userInterfaceService.WriteLine(st);
        }

        // Relay exchanges
        st = this.netTerminal.RelayAgent.ToString();
        if (!string.IsNullOrEmpty(st))
        {
            this.userInterfaceService.WriteLine($"Relay exchanges (Server): ");
            this.userInterfaceService.WriteLine(st);
        }
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetTerminal netTerminal;
    private readonly IRelayControl relayControl;
}
