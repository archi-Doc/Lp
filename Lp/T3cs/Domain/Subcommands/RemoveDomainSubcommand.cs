// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.T3cs.Domain;

[SimpleCommand("remove-domain")]
public class RemoveDomainSubcommand : ISimpleCommandAsync<RemoveDomainSubcommand.Options>
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.SetCreditPeerSubcommand));
    public record Options
    {
        [SimpleOption("Name", Description = "Domain name", Required = true)]
        public string Name { get; init; } = string.Empty;
    }

    private readonly DomainControl domainControl;

    public RemoveDomainSubcommand(DomainControl domainControl)
    {
        this.domainControl = domainControl;
    }

    public async Task RunAsync(Options options, string[] args)
    {
        if (string.IsNullOrEmpty(options.Name))
        {
            return;
        }

        this.domainControl.TryRemoveDomain(options.Name);
    }
}
