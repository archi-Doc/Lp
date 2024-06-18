// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("new-token")]
public class NewTokenSubcommand : ISimpleCommandAsync<NewTokenOptions>
{
    public NewTokenSubcommand(IConsoleService consoleService, ILogger<NewTokenSubcommand> logger, AuthorityVault authorityVault)
    {
        this.consoleService = consoleService;
        this.logger = logger;
        this.authorityVault = authorityVault;
    }

    public async Task RunAsync(NewTokenOptions options, string[] args)
    {
        // Authority key
        var authority = await this.authorityVault.GetAuthority(options.AuthorityName);
        if (authority == null)
        {
            this.logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, options.AuthorityName);
            return;
        }

        var token = new CertificateToken<ConnectionAgreement>(new ConnectionAgreement());
        authority.Sign(token);
        var st = token.ToString();
        this.consoleService.WriteLine(st);

        /*if (RequirementToken.TryParse(st, out var token2))
        {
            this.consoleService.WriteLine($"{token.Equals(token2)}: {token2.ToString()}");
        }*/
    }

    private readonly IConsoleService consoleService;
    private readonly ILogger logger;
    private readonly AuthorityVault authorityVault;
}

public record NewTokenOptions
{
    [SimpleOption("authority", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
