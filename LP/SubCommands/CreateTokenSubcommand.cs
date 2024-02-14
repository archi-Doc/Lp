// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("create-token")]
public class CreateTokenSubcommand : ISimpleCommandAsync<CreateCreditOptions>
{
    public CreateTokenSubcommand(IConsoleService consoleService, ILogger<CreateTokenSubcommand> logger, AuthorityVault authorityVault)
    {
        this.consoleService = consoleService;
        this.logger = logger;
        this.authorityVault = authorityVault;
    }

    public async Task RunAsync(CreateCreditOptions options, string[] args)
    {
        // Authority key
        var authority = await this.authorityVault.GetAuthority(options.AuthorityName);
        if (authority == null)
        {
            this.logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, options.AuthorityName);
            return;
        }

        var token = new AuthenticationToken();
        authority.SignToken(token);
        this.consoleService.WriteLine(token.ToString());
    }

    private readonly IConsoleService consoleService;
    private readonly ILogger logger;
    private readonly AuthorityVault authorityVault;
}

public record CreateTokenOptions
{
    [SimpleOption("authority", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
