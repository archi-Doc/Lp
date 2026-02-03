// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.T3cs.Domain;

[SimpleCommand(CommandName)]
public class AddDomainSubcommand : ISimpleCommandAsync<AddDomainSubcommand.Options>
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.SetCreditPeerSubcommand));
    private const string CommandName = "add-domain";
    private const string OptionName = "DomainAssignment";

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly DomainControl domainControl;

    public record Options
    {
        [SimpleOption(OptionName, Description = "Domain assignment", Required = true)]
        public string DomainAssignment { get; init; } = string.Empty;
    }

    public AddDomainSubcommand(ILogger<AddDomainSubcommand> logger, IUserInterfaceService userInterfaceService, DomainControl domainControl, SimpleParser parser)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.domainControl = domainControl;

        if (parser.TryGetOption(CommandName, OptionName, out var option))
        {
            option.DefaultValueText = StringHelper.SerializeToString(Example.DomainAssignment);
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
