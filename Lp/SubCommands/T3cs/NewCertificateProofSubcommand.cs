// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Stats;
using SimpleCommandLine;

namespace Lp.Subcommands.T3cs;

[SimpleCommand("new-certificate-proof")]
public partial class NewCertificateProofSubcommand : ISimpleCommandAsync<NewCertificateProofSubcommand.Options>
{
    [TinyhandObject(ImplicitMemberNameAsKey = true)]
    public partial record Options
    {
        [SimpleOption("Code", Description = "Code", Required = false)]
        public string Code { get; init; } = string.Empty;

        [SimpleOption("Credit", Description = "Credit", Required = true)]
        public string Credit { get; init; } = string.Empty;
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;
    private readonly LpService lpService;
    private readonly NetStats netStats;

    public NewCertificateProofSubcommand(IUserInterfaceService userInterfaceService, ILogger<NewCertificateProofSubcommand> logger, LpService lpService, NetStats netStats)
    {
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
        this.lpService = lpService;
        this.netStats = netStats;
    }

    public async Task RunAsync(Options options, string[] args)
    {
        var node = this.netStats.GetOwnNetNode();
        if (node is null)
        {// Failed to retrieve the IP address.
            return;
        }

        var seedKey = await this.lpService.GetSeedKeyFromCode(options.Code).ConfigureAwait(false);

        if (!Credit.TryParse(options.Credit, out var credit, out _))
        {
            return;
        }

        if (seedKey is not null)
        {
            var publicKey = seedKey.GetSignaturePublicKey();
            var mergerIndex = credit.GetMergerIndex(ref publicKey);
            if (mergerIndex >= 0)
            {
                var mergedProof = new MergedProof(new(publicKey, 0, credit));
                mergedProof.Sign
            }
        }
    }
}
