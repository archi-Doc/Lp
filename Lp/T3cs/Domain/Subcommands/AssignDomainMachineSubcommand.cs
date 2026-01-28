// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.T3cs.Domain;

[SimpleCommand("assign-domain-machine")]
public class AssignDomainMachineSubcommand : ISimpleCommandAsync<AssignDomainMachineOptions>
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.SetCreditPeerSubcommand));
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly BigMachine bigMachine;

    public AssignDomainMachineSubcommand(ILogger<AssignDomainMachineSubcommand> logger, IUserInterfaceService userInterfaceService, BigMachine bigMachine)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.bigMachine = bigMachine;
    }

    public async Task RunAsync(AssignDomainMachineOptions options, string[] args)
    {
        if (!DomainMachineHelper.TryParseDomainMachineKind(options.Kind, out var kind))
        {
            return;
        }

        // this.bigMachine.DomainMachine.CreateAlways(DomainMachineKind.CreditPeer, options.DomainIdentifier);
    }
}

public record AssignDomainMachineOptions : ShowDomainMachineOptions
{
    [SimpleOption("DomainIdentifier", Description = "Domain identifier", Required = true)]
    public string DomainIdentifier { get; init; } = string.Empty;
}
