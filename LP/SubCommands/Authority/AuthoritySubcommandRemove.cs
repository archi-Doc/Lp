// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

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
        var result = this.Control.Authority.RemoveAuthority(option.Name);

        if (result == AuthorityResult.Success)
        {
            this.logger.TryGet()?.Log(Hashed.Authority.Created, option.Name);
        }
        else if (result == AuthorityResult.NotFound)
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotFound, option.Name);
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandRemove> logger;
}

public record AuthoritySubcommandNameOptions
{
    [SimpleOption("name", Description = "Key name", Required = true)]
    public string Name { get; init; } = string.Empty;
}
