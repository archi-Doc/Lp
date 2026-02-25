// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Data;
using Lp.T3cs;
using Netsphere.Stats;
using SimpleCommandLine;

namespace Lp.Subcommands.T3cs;

[SimpleCommand("new-certificate-proof")]
public partial class NewCertificateProofSubcommand : ISimpleCommandAsync<NewCertificateProofSubcommand.Options>
{
    public record Options
    {
        [SimpleOption("Code", Description = "Code", Required = false)]
        public string Code { get; init; } = string.Empty;

        [SimpleOption("Credit", Description = "Credit", Required = true)]
        public Credit Credit { get; init; } = Credit.UnsafeConstructor();
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;
    private readonly LpService lpService;
    private readonly NetStats netStats;
    private readonly LpSettings lpSettings;

    public NewCertificateProofSubcommand(IUserInterfaceService userInterfaceService, ILogger<NewCertificateProofSubcommand> logger, LpService lpService, NetStats netStats, LpSettings lpSettings)
    {
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
        this.lpService = lpService;
        this.netStats = netStats;
        this.lpSettings = lpSettings;
    }

    public async Task RunAsync(Options options, string[] args)
    {
        var node = this.netStats.GetOwnNetNode();
        if (node is null || !node.Validate())
        {// Failed to retrieve the IP address.
            this.userInterfaceService.WriteLine(Hashed.Error.NoOwnAddress, this.lpSettings.Color.Error);
            return;
        }

        this.userInterfaceService.WriteLine($"NetNode: {node}");

        var seedKey = await this.lpService.GetSeedKeyFromCode(options.Code).ConfigureAwait(false);
        if (seedKey is null)
        {
            return;
        }

        MergedProof? mergedProof = default;
        var publicKey = seedKey.GetSignaturePublicKey();
        var mergerIndex = options.Credit.GetMergerIndex(ref publicKey);
        if (mergerIndex >= 0)
        {// Since the Merger's SeedKey is specified, create a MergedProof and self-sign it.
            this.userInterfaceService.WriteLine($"Merger[{mergerIndex}] SeedKey is specified");
            mergedProof = new MergedProof(new(publicKey, 1, options.Credit));
            if (!seedKey.TrySignAndValidate(mergedProof, 60))
            {
                return;
            }
        }

        if (mergedProof is null)
        {
            return;
        }

        this.logger.TryGet(LogLevel.Information)?.Log(StringHelper.SerializeToString(mergedProof));

        var certificateProof = new CertificateProof(mergedProof, node);
        if (!seedKey.TrySignAndValidate(certificateProof, 60))
        {
            return;
        }

        var st = StringHelper.SerializeToString(certificateProof);
        this.logger.TryGet(LogLevel.Information)?.Log(st);
        var bin = TinyhandSerializer.SerializeObject(certificateProof);
        this.logger.TryGet(LogLevel.Information)?.Log($"{st.Length} {bin.Length}");
    }
}
