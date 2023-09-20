// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

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
        var authority = await this.Control.AuthorityVault.GetAuthority(option.Name);
        if (authority != null)
        {
            this.logger.TryGet()?.Log($"{option.Name}: {authority.ToString()}");
        }
        else
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotAvailable, option.Name);
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandInfo> logger;
}
