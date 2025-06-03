// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

[SimpleCommand("show-authority")]
public class ShowAuthoritySubcommand : ISimpleCommandAsync<AuthoritySubcommandNameOptions>
{
    public ShowAuthoritySubcommand(IConsoleService consoleService, ILogger<ShowAuthoritySubcommand> logger, AuthorityControl authorityControl)
    {
        this.consoleService = consoleService;
        this.logger = logger;
        this.authorityControl = authorityControl;
    }

    public async Task RunAsync(AuthoritySubcommandNameOptions option, string[] args)
    {
        if (!this.authorityControl.Exists(option.AuthorityName))
        {// Not found
            this.logger.TryGet()?.Log(Hashed.Authority.NotFound, option.AuthorityName);
            return;
        }

        var authority = await this.authorityControl.GetAuthority(option.AuthorityName);
        if (authority != null)
        {
            this.consoleService.WriteLine($"{option.AuthorityName}: {authority.ToString()}");
            this.consoleService.WriteLine($"{authority.GetSeedKey().GetSignaturePublicKey().ToString()}");
        }
        else
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotAvailable, option.AuthorityName);
        }
    }

    private readonly IConsoleService consoleService;
    private readonly ILogger logger;
    private readonly AuthorityControl authorityControl;
}
