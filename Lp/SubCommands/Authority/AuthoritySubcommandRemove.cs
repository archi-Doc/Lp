// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("rm")]
public class AuthoritySubcommandRemove : ISimpleCommandAsync<AuthoritySubcommandNameOptions>
{
    public AuthoritySubcommandRemove(ILogger<AuthoritySubcommandRemove> logger, Control control)
    {
        this.Control = control;
        this.logger = logger;
    }

    public async Task RunAsync(AuthoritySubcommandNameOptions option, string[] args)
    {
        var result = this.Control.AuthorityVault.RemoveAuthority(option.AuthorityName);

        if (result == AuthorityResult.Success)
        {
            this.logger.TryGet()?.Log(Hashed.Authority.Removed, option.AuthorityName);
        }
        else if (result == AuthorityResult.NotFound)
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotFound, option.AuthorityName);
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandRemove> logger;
}

public record AuthoritySubcommandNameOptions
{
    [SimpleOption("name", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
