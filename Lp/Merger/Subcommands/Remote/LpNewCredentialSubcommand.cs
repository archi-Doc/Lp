// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

[SimpleCommand("lp-new-credential")]
public class LpNewCredentialSubcommand : ISimpleCommandAsync<LpNewCredentialOptions>
{
    public LpNewCredentialSubcommand(ILogger<LpNewCredentialSubcommand> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand, AuthorityControl authorityVault)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
        this.authorityVault = authorityVault;
    }

    public async Task RunAsync(LpNewCredentialOptions options, string[] args)
    {
        if (await this.nestedcommand.RobustConnection.GetConnection(this.logger) is not { } connection)
        {
            return;
        }

        if (!SignaturePublicKey.TryParse(options.PublicKey, out var publicKey))
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.InvalidPublicKey);
            return;
        }

        if (await this.authorityVault.GetLpAuthority(this.logger) is not { } lpAuthority)
        {
            return;
        }

        var service = connection.GetService<IMergerRemote>();
        var proof = await service.NewCredential(default);
        if (proof is not ValueProof valueProof ||
            !valueProof.ValidateAndVerify())
        {
            return;
        }

        this.logger.TryGet()?.Log($"{valueProof.ToString()}");

        if (!Evidence.TryCreate(valueProof, out var evidence))
        {
            return;
        }

        // Sign
        if (!lpAuthority.TrySignEvidence(evidence, 0))
        {
            return;
        }

        proof = await service.NewCredential(evidence);
        if (proof is not CredentialProof credentialProof ||
            !credentialProof.ValidateAndVerify())
        {
            return;
        }
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
    private readonly AuthorityControl authorityVault;
}

public record LpNewCredentialOptions
{
    [SimpleOption("PublicKey", Description = "Target public key", Required = true)]
    public string PublicKey { get; init; } = string.Empty;
}
