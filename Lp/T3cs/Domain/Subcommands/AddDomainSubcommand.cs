// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.T3cs.Domain;

[SimpleCommand("add-domain")]
public class AddDomainSubcommand : ISimpleCommandAsync<AddDomainOptions>
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.SetCreditPeerSubcommand));
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly DomainControl domainControl;

    public AddDomainSubcommand(ILogger<AddDomainSubcommand> logger, IUserInterfaceService userInterfaceService, DomainControl domainControl, SimpleParser parser)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.domainControl = domainControl;
    }

    public async Task RunAsync(AddDomainOptions options, string[] args)
    {
        var domain = options.DomainAssignment;
        if (string.IsNullOrEmpty(domain))
        {
            return;
        }

        var result = await this.domainControl.AddDomain(domain).ConfigureAwait(false);
    }
}

public record AddDomainOptions
{
    [SimpleOption("DomainAssignment", Description = "Domain assignment", Required = true)]
    public string DomainAssignment { get; init; } = string.Empty;
}
