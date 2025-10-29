// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("show-domain-machine")]
public class ShowDomainMachineSubcommand : ISimpleCommandAsync
{// DomainMachine: CreditPeer, RelayPeer, DataPeer
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly BigMachine bigMachine;

    public ShowDomainMachineSubcommand(ILogger<TemplateSubcommand> logger, IUserInterfaceService userInterfaceService, BigMachine bigMachine)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.bigMachine = bigMachine;
    }

    public async Task RunAsync(string[] args)
    {
        if (this.bigMachine.DomainMachine.Get() is { } machineInterface)
        {
            await machineInterface.Command.Show();
        }
    }
}
