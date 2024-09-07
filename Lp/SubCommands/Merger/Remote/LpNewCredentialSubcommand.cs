// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

[SimpleCommand("lp-new-credential")]
public class LpNewCredentialSubcommand : ISimpleCommandAsync<LpNewCredentialOptions>
{
    public LpNewCredentialSubcommand(ILogger<LpNewCredentialSubcommand> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand, AuthorityVault authorityVault)
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

        if (await this.authorityVault.GetLpAuthority(this.logger) is not { } authority)
        {
            return;
        }

        var service = connection.GetService<IMergerRemote>();
        var r = await service.NewCredential(default);

        this.logger.TryGet()?.Log($"{r.ToString()}");
        this.logger.TryGet()?.Log("New credential");
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
    private readonly AuthorityVault authorityVault;
}

public record LpNewCredentialOptions
{
    [SimpleOption("PublicKey", Description = "Target public key", Required = true)]
    public string PublicKey { get; init; } = string.Empty;
}
