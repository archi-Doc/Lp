// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

[SimpleCommand("show-authority")]
public class ShowAuthoritySubcommand : ISimpleCommandAsync<AuthoritySubcommandNameOptions>
{
    public ShowAuthoritySubcommand(IConsoleService consoleService, ILogger<ShowAuthoritySubcommand> logger, AuthorityControl2 authorityControl)
    {
        this.consoleService = consoleService;
        this.logger = logger;
        this.authorityControl = authorityControl;
    }

    public async Task RunAsync(AuthoritySubcommandNameOptions option, string[] args)
    {
        var authority = await this.authorityControl.GetAuthority(option.AuthorityName);
        if (authority != null)
        {
            this.consoleService.WriteLine($"{option.AuthorityName}: {authority.ToString()}");
        }
        else
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotAvailable, option.AuthorityName);
        }
    }

    private readonly IConsoleService consoleService;
    private readonly ILogger logger;
    private readonly AuthorityControl2 authorityControl;
}
