// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("info")]
public class AuthoritySubcommandInfo : ISimpleCommandAsync<AuthoritySubcommandNameOptions>
{
    public AuthoritySubcommandInfo(ILogger<AuthoritySubcommandInfo> logger, Control control)
    {
        this.Control = control;
        this.logger = logger;
    }

    public async Task RunAsync(AuthoritySubcommandNameOptions option, string[] args)
    {
        var authorityKey = await this.Control.Authority.GetKeyAsync(option.Name);
        if (authorityKey != null)
        {
            this.logger.TryGet()?.Log(option.Name);
            this.logger.TryGet()?.Log(authorityKey.ToString());
        }
        else
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotAvailable, option.Name);
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandInfo> logger;
}
