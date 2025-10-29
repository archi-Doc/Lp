// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs.Domain;
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
        var kind = DomainMachineKind.CreditPeer;
        if (this.bigMachine.DomainMachine.TryGet(kind, out var machineInterface))
        {
            await machineInterface.Command.Show();
        }
    }
}
