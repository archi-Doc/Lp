// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using SimpleCommandLine;

namespace Lp.T3cs.Domain;

[SimpleCommand(CommandName)]
public class AddDomainSubcommand : ISimpleCommandAsync<AddDomainSubcommand.Options>
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.SetCreditPeerSubcommand));
    private const string CommandName = "add-domain";
    private const string OptionName = "DomainAssignment";

    public record Options
    {
        [SimpleOption("Name", Description = "Domain name", Required = true)]
        public string Name { get; init; } = string.Empty;

        [SimpleOption("Code", Description = "Domain name", Required = true)]
        public string Code { get; init; } = string.Empty;

        [SimpleOption("CertificateProof", Description = "", Required = true)]
        public CertificateProof CertificateProof { get; init; } = CertificateProof.UnsafeConstructor();
    }

    // private readonly ILogger logger;
    // private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;
    private readonly DomainControl domainControl;

    public AddDomainSubcommand(LpService lpService, DomainControl domainControl, SimpleParser parser)
    {
        this.lpService = lpService;
        this.domainControl = domainControl;

        if (parser.TryGetOption(CommandName, OptionName, out var option))
        {
            option.Description = StringHelper.SerializeToString(Example.DomainAssignment);
        }
    }

    public async Task RunAsync(Options options, string[] args)
    {
        var seedKey = await this.lpService.GetSeedKeyFromCode(options.Code).ConfigureAwait(false);
        if (seedKey is null)
        {
            return;
        }

        //
        var b = options.CertificateProof.ValidateAndVerify(ValidationOption.IgnoreExpiration);

        //var result = await this.domainControl.AddDomain(domain).ConfigureAwait(false);
    }
}
