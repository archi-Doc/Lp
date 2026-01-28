// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.T3cs.Domain;

[SimpleCommand("show-domain-machine")]
public class ShowDomainMachineSubcommand : ISimpleCommandAsync<ShowDomainMachineOptions>
{// DomainMachine: CreditPeer, RelayPeer, DataPeer
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly BigMachine bigMachine;

    public ShowDomainMachineSubcommand(ILogger<ShowDomainMachineSubcommand> logger, IUserInterfaceService userInterfaceService, BigMachine bigMachine)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.bigMachine = bigMachine;
    }

    public async Task RunAsync(ShowDomainMachineOptions options, string[] args)
    {
        if (!DomainMachineHelper.TryParseDomainMachineKind(options.Kind, out var kind))
        {
            return;
        }

        /*if (this.bigMachine.DomainMachine.TryGet(kind, out var machineInterface))
        {
            await machineInterface.Command.Show();
        }*/
    }
}

public record ShowDomainMachineOptions
{
    [SimpleOption("Kind", Description = "Domain machine kind (CreditMerger, CreditPeer...)", Required = true)]
    public string Kind { get; init; } = string.Empty;
}
