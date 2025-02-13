// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

[SimpleCommand("lp-new-credential", Alias = "lpnc")]
public class LpNewCredentialSubcommand : ISimpleCommandAsync<LpNewCredentialOptions>
{
    public LpNewCredentialSubcommand(ILogger<LpNewCredentialSubcommand> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand, AuthorityControl authorityControl, Credentials credentials)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
        this.authorityControl = authorityControl;
        this.credentials = credentials;
    }

    public async Task RunAsync(LpNewCredentialOptions options, string[] args)
    {
        if (await this.nestedcommand.RobustConnection.Get(this.logger) is not { } connection)
        {
            return;
        }

        if (!SignaturePublicKey.TryParse(options.PublicKey, out var publicKey, out _))
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.InvalidPublicKey);
            return;
        }

        if (await this.authorityControl.GetLpSeedKey(this.logger) is not { } seedKey)
        {
            return;
        }

        var service = connection.GetService<IMergerRemote>();
        var token = CertificateToken<Value>.CreateAndSign(new Value(publicKey, 1, LpConstants.LpCredit), seedKey, connection);
        var credentialProof = await service.NewCredentialProof(token);
        if (credentialProof is null ||
            !credentialProof.ValidateAndVerify() ||
            !credentialProof.GetSignatureKey().Equals(publicKey))
        {
            return;
        }

        CredentialEvidence.TryCreate(credentialProof, seedKey, out var evidence);
        if (evidence?.ValidateAndVerify() != true)
        {
            return;
        }

        this.credentials.MergerCredentials.Add(evidence);

        this.logger.TryGet()?.Log($"{evidence}");
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
    private readonly AuthorityControl authorityControl;
    private readonly Credentials credentials;
}

public record LpNewCredentialOptions
{
    [SimpleOption("PublicKey", Description = "Target public key", Required = true)]
    public string PublicKey { get; init; } = string.Empty;
}
