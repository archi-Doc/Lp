// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.T3cs.Domain;

[SimpleCommand(CommandName)]
public class AddDomainSubcommand : ISimpleCommandAsync<AddDomainSubcommand.Options>
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.SetCreditPeerSubcommand));
    private const string CommandName = "add-domain";
    private const string OptionName = "DomainAssignment";

    public record Options
    {
        [SimpleOption(OptionName, Description = "", Required = true)]
        public string DomainAssignment { get; init; } = string.Empty;
    }

    // private readonly ILogger logger;
    // private readonly IUserInterfaceService userInterfaceService;
    private readonly DomainControl domainControl;

    public AddDomainSubcommand(DomainControl domainControl, SimpleParser parser)
    {
        this.domainControl = domainControl;

        if (parser.TryGetOption(CommandName, OptionName, out var option))
        {
            option.Description = StringHelper.SerializeToString(Example.DomainAssignment);
        }
    }

    public async Task RunAsync(Options options, string[] args)
    {
        var domain = options.DomainAssignment;
        if (string.IsNullOrEmpty(domain))
        {
            return;
        }

        var result = await this.domainControl.AddDomain(domain).ConfigureAwait(false);
    }
}
