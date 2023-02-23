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
        var authorityKey = await this.Control.Authority.GetKey(option.Name);
        if (authorityKey != null)
        {
            this.logger.TryGet()?.Log($"{option.Name}: {authorityKey.ToString()}");
        }
        else
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotAvailable, option.Name);
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandInfo> logger;
}
