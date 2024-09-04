// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

[SimpleCommand("remove-authority")]
public class RemoveAuthoritySubcommand : ISimpleCommandAsync<AuthoritySubcommandNameOptions>
{
    public RemoveAuthoritySubcommand(ILogger<RemoveAuthoritySubcommand> logger, Control control)
    {
        this.control = control;
        this.logger = logger;
    }

    public async Task RunAsync(AuthoritySubcommandNameOptions option, string[] args)
    {
        var result = this.control.AuthorityVault.RemoveAuthority(option.AuthorityName);

        if (result == AuthorityResult.Success)
        {
            this.logger.TryGet()?.Log(Hashed.Authority.Removed, option.AuthorityName);
        }
        else if (result == AuthorityResult.NotFound)
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotFound, option.AuthorityName);
        }
    }

    private readonly Control control;
    private readonly ILogger logger;
}

public record AuthoritySubcommandNameOptions
{
    [SimpleOption("Name", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
